using System;
using System.IO;
using System.Text;

namespace ScriptDecoder
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("Usage: ScriptDecoder.exe <script_file>");
                return;
            }

            string fileInput = args[0];

            var br = new BinaryReader(new FileStream(fileInput, FileMode.Open));
            var bw =
                new StreamWriter(new FileStream(Path.GetFileNameWithoutExtension(fileInput) + ".txt",
                                                FileMode.Create));

            int content;

            // Check whether the file is in new format.
            // The most significant thing is, the new format always have the magic "BurikoCompiledScript".
            // The difference between old and new is that, the old one DOES NOT have the HEADER which has
            // the length discribed at [0x1C] as a DWORD.
            if (br.ReadBytes(20) == new byte[]
                {
                    0x42, 0x75, 0x72, 0x69, 0x6B, 0x6F, 0x43, 0x6F, 0x6D, 0x70,
                    0x69, 0x6C, 0x65, 0x64, 0x53, 0x63, 0x72, 0x69, 0x70, 0x74
                })
            {
                content = 0x1C + BitConverter.ToInt32(br.ReadBytes((int) br.BaseStream.Length), 0x1C);
            }
            else
            {
                content = 0;
            }

            br.BaseStream.Position = content;

            // Get the text offset.
            // The offset is always next to 0x00000001, which located at [0x18] (HEADER length not included).
            int intFirstValidOffset = BitConverter.ToInt32(
                br.ReadBytes((int) br.BaseStream.Length - content), 0x18);

            br.BaseStream.Position = content;

            byte[] bytesFileInput = br.ReadBytes((int) br.BaseStream.Length - content);

            br.Close();

            // Text offset is always next to 0x00000003.
            // To get the actual offset ,combine this value with HEADER length.
            int intTextOffsetLabel = FindBytes(bytesFileInput, new byte[] {0, 3, 0, 0, 0}, 0);

            while (intTextOffsetLabel != -1)
            {
                int intTextOffset = BitConverter.ToInt32(bytesFileInput, intTextOffsetLabel + 5);
                byte[] bytesTextBlock = CopyBlock(bytesFileInput, intTextOffset,
                                                  (FindByte(bytesFileInput, 0, intTextOffset) - intTextOffset));

                if (intTextOffset > intFirstValidOffset)
                {
                    if (bytesTextBlock != null)
                    {
                        // BGI treat 0x0A as new line.
                        string strText = Encoding.GetEncoding(932)
                                                 .GetString(bytesTextBlock)
                                                 .Replace("\n", @"\n");
                        bw.WriteLine("<{0},{1},{2}>{3}", intTextOffsetLabel, intTextOffset,
                                     bytesTextBlock.Length, strText);
                    }
                }

                intTextOffsetLabel = FindBytes(bytesFileInput, new byte[] {0, 3, 0, 0, 0},
                                               intTextOffsetLabel + 1);
            }

            bw.Close();
            //Console.ReadLine();
        }

        private static int FindByte(byte[] bytesInput, byte pattern, int intStart)
        {
            for (int i = intStart; i < bytesInput.Length; i++)
            {
                if (i <= 0)
                    return -1;

                if (bytesInput[i] == pattern)
                    return i;
            }
            return -1;
        }

        private static int FindBytes(byte[] bytesInput, byte[] bytesFind, int intStart)
        {
            for (int index = intStart; index <= (bytesInput.Length - bytesFind.Length); index++)
            {
                if (BitConverter.ToString(CopyBlock(bytesInput, index, bytesFind.Length)) ==
                    BitConverter.ToString(bytesFind))
                    return index;
            }
            return -1;
        }

        private static byte[] CopyBlock(byte[] bytesOrg, int intStart, int intLength)
        {
            try
            {
                var byteOutput = new byte[intLength];

                for (int i = 0; i < intLength; i++)
                {
                    byteOutput[i] = bytesOrg[intStart + i];
                }
                return byteOutput;
            }
            catch (Exception e)
            {
                // In the processing, there may be many Overflow Exceptions.
                // We should just ignore them.
                Console.WriteLine("{0}{1}", e.Message, intStart);
                return null;
            }
        }

    }
}
