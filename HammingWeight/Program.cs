using System;
using System.IO;

namespace HammingWeight
{
    class Program
    {
        static void Main(string[] args)
        {
            string file = "big.txt";
            GetStatistics(file, GetBinaryFile(file));

            file = "bible.txt";
            GetStatistics(file, GetBinaryFile(file));

            file = "world192.txt";
            GetStatistics(file, GetBinaryFile(file));

            file = "randomfromchar.txt";
            int size = 100000;
            string str = "";
            Random rnd = new Random();
            for (int i = 0; i < size; i++)
                str += (char)rnd.Next(0, 256);
            File.WriteAllText(file, str);
            GetStatistics(file, GetBinaryFile(file));

            file = "randomfrombinary.txt";
            size = 100000;
            str = "";
            for (int i = 0; i < size; i++)
                str += (char)rnd.Next(48, 50);
            string str2 = "";
            while (str.Length >= 8)
            {
                str2 += (char)Convert.ToInt32(str.Substring(0, 8), 2);
                str = str.Substring(8);
            }
            File.WriteAllText(file, str2);
            GetStatistics(file, GetBinaryFile(file));

            Console.ReadLine();
        }

        private static byte[] GetBinaryFile(string filename)
        {
            byte[] bytes;
            using (FileStream file = new FileStream(filename, FileMode.Open, FileAccess.Read))
            {
                bytes = new byte[file.Length];
                file.Read(bytes, 0, (int)file.Length);
            }
            return bytes;
        }

        private static void GetStatistics(string title, byte[] binaryFile)
        {
            Console.WriteLine(title);
            decimal one = 0;
            decimal zero = 0;
            decimal total = 0;
            int pad = 8;
            for (int i = 0; i < binaryFile.Length; i++)
            {
                string binaryStr = Convert.ToString(binaryFile[i], 2).PadLeft(pad, '0');
                total += pad;
                for (int b = 0; b < binaryStr.Length; b++)
                {
                    if (binaryStr[b] == '0')
                        zero++;
                    else if (binaryStr[b] == '1')
                        one++;
                }
            }

            Console.WriteLine("zero " + zero + " [" + zero / total + "]");
            Console.WriteLine("one " + one + " [" + one / total + "]");
            Console.WriteLine("total " + total);
            Console.WriteLine("-------------");
        }
    }
}