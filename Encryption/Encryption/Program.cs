/*                              █░░░███░░░░████░░░░
 * Author: Dylan McBean-Kyle    ░██████░███░██░████
 * Date: 03/11/2019             ░█░░░██░███░███░░░█
 * Script Title: Encryption     ░███░██░███░██████░
 *                              █░░░███░░░░███░░░░█
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.IO;
using System.Threading;
using System.Diagnostics;
using System.Windows.Forms;
using System.Security.Cryptography;
using System.IO.Compression;

namespace Encryption
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            //Get files from directory
            string[] filePaths;
            if (args.Length == 0)
                filePaths = Directory.GetFiles(@"Files\", "*", SearchOption.AllDirectories).OrderBy(f => new FileInfo(f).Length).ToArray();
            else
                filePaths = Directory.GetFiles(args[0], "*", SearchOption.AllDirectories).OrderBy(f => new FileInfo(f).Length).ToArray();
            //Getting Password
            Console.Write("Enter password: ");
            string Password = Console.ReadLine();
            string EDType;

            //getting Excryption/Decryption
            do
            {
                Console.Write("(e)ncryption/(d)ecryption: ");
                EDType = Console.ReadLine();
            } while (EDType != "e" && EDType != "d");

            //Main Code Run
            Stopwatch stopwatch; //creates and start the instance of Stopwatch
            int threads = 64;
            List<String>[] SectionFiles = new List<String>[threads];

            Console.WriteLine("Splitting Files onto Threads");
            for (int i = 0; i < SectionFiles.Length; i++)
            {
                SectionFiles[i] = new List<String>();
            }
            int indexnum = 0;
            for (int i = 0; i < filePaths.Length; i++)
            {
                SectionFiles[indexnum].Add(filePaths[i]);
                indexnum = (indexnum + 1) % threads;
            }

            //Setting Threads
            Thread[] threadsArray = new Thread[threads];

            Console.WriteLine($"Total files: {filePaths.Length}\nTotal threads: {Math.Min(threadsArray.Length * filePaths.Length, 4096)}\nPress Any key to Begin");
            Console.ReadKey();

            stopwatch = Stopwatch.StartNew();
            if (EDType == "e")
            {
                for (int i = 0; i < threadsArray.Length; i++)
                {
                    int t = i;
                    threadsArray[i] = new Thread(() => Encrypt(t.ToString(), Password, SectionFiles[t].ToArray(), 1));
                }
                for (int i = 0; i < threadsArray.Length; i++)
                {
                    threadsArray[i].Start();
                }
                for (int i = 0; i < threadsArray.Length; i++)
                {
                    threadsArray[i].Join();
                }
            }
            else
            {
                for (int i = 0; i < threadsArray.Length; i++)
                {
                    int t = i;
                    threadsArray[i] = new Thread(() => Decrypt(t.ToString(), Password, SectionFiles[t].ToArray(), 1));
                }
                for (int i = 0; i < threadsArray.Length; i++)
                {
                    threadsArray[i].Start();
                }
                for (int i = 0; i < threadsArray.Length; i++)
                {
                    threadsArray[i].Join();
                }
            }
            stopwatch.Stop();
            Clipboard.SetText(stopwatch.Elapsed.ToString());
            // Keep the console window open in debug mode.
            Console.WriteLine($"Elapsed Time {stopwatch.Elapsed}\nPress any key to exit.");
            Console.ReadKey();
        }

        static void Encrypt(String thread, String Password, String[] filePaths, int index)
        {
            foreach (string filePath in filePaths)
            {
                Random rnd = new Random(Password.GetHashCode());
                string[] lines = System.IO.File.ReadAllLines(filePath);

                //Aditional Information
                String name = Path.GetFileName(filePath);
                byte[] byt = System.Text.Encoding.UTF8.GetBytes(name);
                var strModified = Convert.ToBase64String(byt);

                Directory.CreateDirectory(Path.GetDirectoryName(filePath) + "\\" + strModified);
                File.Move(filePath, Path.GetDirectoryName(filePath) + "\\" + strModified + "\\" + Path.GetFileName(filePath));
                ZipFile.CreateFromDirectory(Path.GetDirectoryName(filePath) + "\\" + strModified, Path.GetDirectoryName(filePath) + "\\" + strModified + ".zp");
                Directory.Delete(Path.GetDirectoryName(filePath) + "\\" + strModified, true);
                string filePath_ = Path.GetDirectoryName(filePath) + "\\" + strModified + ".zp";

                name = Path.GetFileName(filePath_);
                byt = System.Text.Encoding.UTF8.GetBytes(name);
                strModified = Convert.ToBase64String(byt);
                #region "dictionaries"
                //Dictionaries
                var ByteSwap = new Dictionary<int, int>();
                var BitSwap = new Dictionary<string, string>();
                //setting byteSwap dictionary
                int[] intArray = new int[256];
                for (int a = 0; a < 256; a++)
                {
                    intArray[a] = a;
                }
                intArray = intArray.OrderBy(x => rnd.Next()).ToArray();
                for (int a = 0; a < intArray.Length; a++)
                {
                    ByteSwap.Add(a, intArray[a]);
                }
                //setting bitSwap dictionary
                string[] strArray = new string[256];
                for (int a = 0; a < 256; a++)
                {
                    strArray[a] = Convert.ToString(a, 2).PadLeft(8, '0');
                }
                strArray = strArray.OrderBy(x => rnd.Next()).ToArray();
                for (int a = 0; a < strArray.Length; a++)
                {
                    BitSwap.Add(Convert.ToString(a, 2).PadLeft(8, '0'), strArray[a]);
                }
                #endregion

                // Display the file contents by using a foreach loop.
                System.Console.WriteLine($"Thread[{thread}] {index} / {filePaths.Length} - Encrypting {filePath_}");
                List<byte> bytes = File.ReadAllBytes(filePath_).ToList();
                int error = 0;
                while (bytes.Count % 64 != 0)
                {
                    bytes.Add((byte)0);
                    error++;
                }
                if (bytes.Count > 0)
                    bytes[bytes.Count - 1] = (byte)error;

                List<byte>[] ByteArrays = new List<byte>[64];

                //Splitting bytes into threads
                for (int i = 0; i < ByteArrays.Length; i++)
                {
                    ByteArrays[i] = new List<byte>();
                }
                int indexnum = 0;
                for (int i = 0; i < bytes.Count; i++)
                {
                    ByteArrays[indexnum].Add(bytes[i]);
                    indexnum = (indexnum + 1) % 64;
                }

                //Byte Threads
                Thread[] byteThreadsArray = new Thread[64];
                int loopLength = rnd.Next(10, 20);

                for (int i = 0; i < byteThreadsArray.Length - 1; i++)
                {
                    int t = i;
                    byteThreadsArray[i] = new Thread(() => ByteArrays[t] = InnerEncrypt(loopLength, ByteArrays[t], ByteSwap, BitSwap, Password));
                }
                for (int i = 0; i < byteThreadsArray.Length - 1; i++)
                {
                    byteThreadsArray[i].Start();
                }
                for (int i = 0; i < byteThreadsArray.Length - 1; i++)
                {
                    byteThreadsArray[i].Join();
                }
                bytes = new List<byte>();
                for (int j = 0; j < ByteArrays[0].Count; j++)
                    for (int i = 0; i < ByteArrays.Length; i++)
                        bytes.Add(ByteArrays[i][j]);

                //Write File
                try
                {
                    File.WriteAllBytes(filePath_, bytes.ToArray());
                    String pathName = Path.GetDirectoryName(filePath_);
                    File.Move(filePath_, $"{pathName}/{strModified}.PEFF");
                }
                catch (Exception ex)
                {
                    StreamWriter sw = File.AppendText($"Logs/Log_{thread}.log");
                    sw.Write($"\nERROR - {ex}, File - {filePath_}");
                    sw.Close();
                }
                index++;
            }
        }

        static List<byte> InnerEncrypt(int loopLength, List<byte> bytes, Dictionary<int, int> ByteSwap, Dictionary<string, string> BitSwap, String Password)
        {
            if (bytes.Count > 0)
                for (int l = loopLength; l >= 0; l--)
                {
                    //Switch Bytes
                    for (int i = 0; i < bytes.Count; i++)
                    {
                        byte Byte = bytes[i];
                        bytes[i] = (byte)ByteSwap[Byte];
                    }

                    //Switch Bits
                    for (int i = 0; i < bytes.Count; i++)
                    {
                        byte Byte = bytes[i];
                        bytes[i] = (byte)Convert.ToInt32(BitSwap[Convert.ToString(Byte, 2).PadLeft(8, '0')], 2);
                    }

                    //Xor Bytes
                    for (int i = 0; i < bytes.Count; i++)
                    {
                        byte Byte = bytes[i];
                        bytes[i] = (byte)(Byte ^ (int)Password[i % Password.Length]);
                    }

                    //Flip Byte Array
                    bytes.Reverse();
                }
            return bytes;
        }

        static void Decrypt(String thread, String Password, String[] filePaths, int index)
        {
            foreach (string filePath in filePaths)
            {
                Random rnd = new Random(Password.GetHashCode());
                string[] lines = System.IO.File.ReadAllLines(filePath);
                String name = Path.GetFileName(filePath).Replace(".PEFF", "");
                var strModified = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(name));

                // Display the file contents by using a foreach loop.
                System.Console.WriteLine($"Thread[{thread}] {index} / {filePaths.Length} - Decrypting {filePath}");

                #region "dictionaries"
                //Dictionaries
                var ByteSwap = new Dictionary<int, int>();
                var BitSwap = new Dictionary<string, string>();
                //setting byteSwap dictionary
                int[] intArray = new int[256];
                for (int a = 0; a < 256; a++)
                {
                    intArray[a] = a;
                }
                intArray = intArray.OrderBy(x => rnd.Next()).ToArray();
                for (int a = 0; a < intArray.Length; a++)
                {
                    ByteSwap.Add(a, intArray[a]);
                }
                ByteSwap = ByteSwap.ToDictionary(kp => kp.Value, kp => kp.Key);
                //setting bitSwap dictionary
                string[] strArray = new string[256];
                for (int a = 0; a < 256; a++)
                {
                    strArray[a] = Convert.ToString(a, 2).PadLeft(8, '0');
                }
                strArray = strArray.OrderBy(x => rnd.Next()).ToArray();
                for (int a = 0; a < strArray.Length; a++)
                {
                    BitSwap.Add(Convert.ToString(a, 2).PadLeft(8, '0'), strArray[a]);
                }
                BitSwap = BitSwap.ToDictionary(kp => kp.Value, kp => kp.Key);
                #endregion

                List<byte> bytes = File.ReadAllBytes(filePath).ToList();

                List<byte>[] ByteArrays = new List<byte>[64];

                //Splitting bytes into threads
                for (int i = 0; i < ByteArrays.Length; i++)
                {
                    ByteArrays[i] = new List<byte>();
                }
                int indexnum = 0;
                for (int i = 0; i < bytes.Count; i++)
                {
                    ByteArrays[indexnum].Add(bytes[i]);
                    indexnum = (indexnum + 1) % 64;
                }

                //Byte Threads
                Thread[] byteThreadsArray = new Thread[64];
                int loopLength = rnd.Next(10, 20);

                for (int i = 0; i < byteThreadsArray.Length - 1; i++)
                {
                    int t = i;
                    byteThreadsArray[i] = new Thread(() => ByteArrays[t] = InnerDecrypt(loopLength, ByteArrays[t], ByteSwap, BitSwap, Password));
                }
                for (int i = 0; i < byteThreadsArray.Length - 1; i++)
                {
                    byteThreadsArray[i].Start();
                }
                for (int i = 0; i < byteThreadsArray.Length - 1; i++)
                {
                    byteThreadsArray[i].Join();
                }
                bytes = new List<byte>();
                for (int j = 0; j < ByteArrays[0].Count; j++)
                    for (int i = 0; i < ByteArrays.Length; i++)
                        bytes.Add(ByteArrays[i][j]);
                if (bytes[bytes.Count - 2] == 0 && bytes[bytes.Count - 1] != 0 || bytes[bytes.Count - 1] == 1)
                    bytes.RemoveRange(bytes.Count - bytes[bytes.Count - 1], bytes[bytes.Count - 1]);

                //Write File
                try
                {
                    File.WriteAllBytes(filePath, bytes.ToArray());
                    String pathName = Path.GetDirectoryName(filePath);
                    File.Move(filePath, $"{pathName}/{strModified}");
                    ZipFile.ExtractToDirectory(pathName + "\\" + strModified, pathName + ".\\");
                    File.Delete(pathName + "\\" + strModified);
                }
                catch (Exception ex)
                {
                    StreamWriter sw = File.AppendText($"Logs/Log_{thread}.log");
                    sw.Write($"\nERROR - {ex}, File - {filePath}");
                    sw.Close();
                }
                index++;
            }
        }

        static List<byte> InnerDecrypt(int loopLength, List<byte> bytes, Dictionary<int, int> ByteSwap, Dictionary<string, string> BitSwap, String Password)
        {
            if (bytes.Count > 0)
                for (int l = loopLength; l >= 0; l--)
                {
                    //Flip Byte Array
                    bytes.Reverse();

                    //Xor Bytes
                    for (int i = 0; i < bytes.Count; i++)
                    {
                        byte Byte = bytes[i];
                        bytes[i] = (byte)(Byte ^ (int)Password[i % Password.Length]);
                    }

                    //Switch Bits
                    for (int i = 0; i < bytes.Count; i++)
                    {
                        byte Byte = bytes[i];
                        bytes[i] = (byte)Convert.ToInt32(BitSwap[Convert.ToString(Byte, 2).PadLeft(8, '0')], 2);
                    }

                    //Switch Bytes
                    for (int i = 0; i < bytes.Count; i++)
                    {
                        byte Byte = bytes[i];
                        bytes[i] = (byte)ByteSwap[Byte];
                    }
                }
            return bytes;
        }
    }
}
