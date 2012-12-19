using System;
using System.IO;
using System.Text;

namespace ScriptEncoder
{
    class Program
    {
        private static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("Usage: ScriptEncoder.exe <input_file.txt>");
                Console.ReadKey();
                return;
            }

            string strFileInput = args[0];

            // Load script file.
            var br = new BinaryReader(new FileStream(strFileInput.Replace(".txt", ""), FileMode.Open));

            br.BaseStream.Position = 0x1C;
            int headerLength = br.ReadInt32();
            br.BaseStream.Position += 0x24 + headerLength - 4;
            int textStartPostion = br.ReadInt32();
            br.BaseStream.Position = 0;

            // Write Header
            var headerBuffer = br.ReadBytes(0x1C + headerLength);

            var msCommands = new MemoryStream(br.ReadBytes(textStartPostion));

            #region text

            var msTexts = new MemoryStream();

            #region fix for first text

            while (br.PeekChar() != 0x00)
            {
                msTexts.Write(br.ReadBytes(1), 0, 1);
            }
            msTexts.WriteByte(0);

            #endregion fix for first text

            var sr = new StreamReader(strFileInput, Encoding.UTF8, true);
            while (!sr.EndOfStream)
            {
                string line = sr.ReadLine();

                if (!line.StartsWith("<"))
                    continue;

                int[] infos = GetLineInfo(line);

                msCommands.Position = infos[0];
                msCommands.Write(BitConverter.GetBytes(msTexts.Position + msCommands.Length), 0, 4);

                byte[] text = Encoding.GetEncoding(936).GetBytes(GetText(line));
                msTexts.Write(text, 0, text.Length);
                msTexts.WriteByte(0x00);
            }

            #endregion text

            var bw = new BinaryWriter(new FileStream(strFileInput + ".new", FileMode.Create));
            bw.Write(headerBuffer);
            bw.Write(msCommands.ToArray());
            bw.Write(msTexts.ToArray());

            br.Close();
            bw.Close();
        }

        private static int[] GetLineInfo(string line)
        {
            string[] s = line.Substring(line.IndexOf('<') + 1, line.IndexOf('>') - line.IndexOf('<') - 1).Split(',');

            var i = new int[3];
            i[0] = Int32.Parse(s[0]);
            i[1] = Int32.Parse(s[1]);
            i[2] = Int32.Parse(s[2]);

            return i;
        }

        private static string GetText(string line)
        {
            return line.Substring(line.IndexOf('>') + 1).Replace(@"\n", "\x0A");
        }

        public static Int32 ReadInt32(MemoryStream ms)
        {
            return BitConverter.ToInt32(ReadBytes(ms, 4), 0);
        }

        public static byte[] ReadBytes(MemoryStream ms, int length)
        {
            var b = new byte[length];
            ms.Read(b, 0, length);
            return b;
        }
    }
}
