﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.ComponentModel;
using DynamicTypeDescriptor;
using System.Linq.Expressions;
using System.Diagnostics;
using System.IO.Compression;
using UnitsNet;

namespace CaeGlobals
{
    [Serializable]
    public static class Tools
    {
        private static readonly float _oneThird = 1f / 3f;
        private static readonly float _twoPiThirds = 2f * (float)Math.PI / 3f;
        private static readonly float _fourPiThirds = 4f * (float)Math.PI / 3f;
        //
        private static readonly float _radToDeg = (float)(180f / Math.PI);
        private static readonly float _degToRad = (float)(Math.PI / 180f);
        // Read clone from File
        public static T LoadDumpFromFile<T>(string fileName)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                FileStream fs = new FileStream(fileName, FileMode.Open);
                byte[] bytes = new byte[fs.Length];
                fs.Read(bytes, 0, (int)fs.Length);
                stream.Write(bytes, 0, (int)fs.Length);
                fs.Close();
                stream.Position = 0;
                BinaryFormatter formatter = new BinaryFormatter();
                return (T)formatter.Deserialize(stream);
            }
        }
        public static T LoadDumpFromFile<T>(BinaryReader br)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                long length = br.ReadInt64();
                byte[] bytes = br.ReadBytes((int)length);
                stream.Write(bytes, 0, (int)length);
                stream.Position = 0;
                BinaryFormatter formatter = new BinaryFormatter();
                return (T)formatter.Deserialize(stream);
            }
        }
        public static async Task<T> LoadDumpFromFileAsync<T>(string fileName)
        {
            T a = default;
            await Task.Run(() => a = LoadDumpFromFile<T>(fileName));
            return a;
        }
        //
        public static void WriteIntToFileStream(FileStream fileStream, int value)
        {
            byte[] buffer = BitConverter.GetBytes(value);
            if (buffer.Length != 4) throw new NotSupportedException();
            fileStream.Write(buffer, 0, 4);
        }
        public static void WriteStringToFileStream(FileStream fileStream, string value)
        {
            WriteIntToFileStream(fileStream, value.Length);
            WriteStringToFileStream(fileStream, value, value.Length);
        }
        public static void WriteStringToFileStream(FileStream fileStream, string value, int numOfBytes)
        {
            byte[] byteData = Encoding.ASCII.GetBytes(value);
            byte[] buffer = new byte[numOfBytes];
            byteData.CopyTo(buffer, 0);
            //
            fileStream.Write(buffer, 0, numOfBytes);
        }
        public static int ReadIntFromFileStream(FileStream fileStream)
        {
            byte[] buffer = new byte[4];
            fileStream.Read(buffer, 0, buffer.Length);
            return BitConverter.ToInt32(buffer, 0);
        }
        public static string ReadStringFromFileStream(FileStream fileStream)
        {
            int numOfBytes = ReadIntFromFileStream(fileStream);
            return ReadStringFromFileStream(fileStream, numOfBytes);

        }
        public static string ReadStringFromFileStream(FileStream fileStream, int numOfBytes)
        {
            byte[] buffer = new byte[numOfBytes];
            fileStream.Read(buffer, 0, buffer.Length);
            return Encoding.ASCII.GetString(buffer);
        }
        //
        public static string GetLocalPath(string path)
        {
            string startUpPath = System.Windows.Forms.Application.StartupPath;
            if (path.StartsWith(startUpPath))
            {
                return "#" + path.Substring(startUpPath.Length);
            }
            return path;
        }
        public static string GetGlobalPath(string path)
        {
            if (path != null && path.StartsWith("#"))
            {
                string startUpPath = System.Windows.Forms.Application.StartupPath;
                return Path.Combine(startUpPath, path.Substring(1).TrimStart('\\'));
            }
            return path;
        }
        public static string GetNonExistentRandomFileName(string path, string extension = "")
        {
            string hash;
            bool repeat;
            string[] allFiles = Directory.GetFiles(path);
            //
            do
            {
                hash = GetRandomString(8);
                //
                repeat = false;
                foreach (var fileName in allFiles)
                {
                    if (fileName.StartsWith(hash))
                    {
                        repeat = true;
                        break;
                    }
                }
            }
            while (repeat);
            //
            return Path.Combine(path, Path.ChangeExtension(hash, extension));
        }
        // Compression
        public static byte[] Compress(Stream input, CompressionLevel compressionLevel)
        {
            using (var compressStream = new MemoryStream())
            using (var compressor = new DeflateStream(compressStream, compressionLevel))
            {
                input.CopyTo(compressor);
                compressor.Close();
                return compressStream.ToArray();
            }
        }
        public static Stream Decompress(Stream input)
        {
            var output = new MemoryStream();
            //
            using (var decompressor = new DeflateStream(input, CompressionMode.Decompress))
            {
                decompressor.CopyTo(output);
            }
            //
            output.Position = 0;
            return output;
        }
        // Rounding
        public static double RoundToSignificantDigits(double d, int digits)
        {
            //double[] stdValues = new double[] { 0.1, 0.2, 0.25, 0.5 };
            if (d == 0.0)
            {
                return 0.0;
            }
            else
            {
                double leftSideNumbers = Math.Floor(Math.Log10(Math.Abs(d))) + 1;
                double scale = Math.Pow(10, leftSideNumbers);
                double result = scale * Math.Round(d / scale, digits, MidpointRounding.AwayFromZero);
                // Clean possible precision error.
                if ((int)leftSideNumbers >= digits)
                {
                    return Math.Round(result, 0, MidpointRounding.AwayFromZero);
                }
                else
                {
                    int roundingDigits = digits - (int)leftSideNumbers;
                    roundingDigits = Math.Max(0, Math.Min(roundingDigits, 15));
                    return Math.Round(result, roundingDigits, MidpointRounding.AwayFromZero);
                }
            }
        }
        // Windows version
        public static string GetWindowsName()
        {
            var reg = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion");
            string productName = (string)reg.GetValue("ProductName");
            return productName;
        }
        // Check if it's Windows 8.1
        public static bool IsWindows10orNewer()
        {
            try
            {
                string[] tmp = GetWindowsName().Split(new string[] {" "}, StringSplitOptions.RemoveEmptyEntries);
                double version;

                if (tmp.Length > 2 && double.TryParse(tmp[1], out version) && version >= 10) return true;
                else return false;
            }
            catch
            {
                return false;
            }
        }

        // File locked
        public static bool IsFileLocked(string fileName)
        {
            FileInfo fileInfo = new FileInfo(fileName);
            FileStream stream = null;

            try
            {
                stream = fileInfo.Open(FileMode.Open, FileAccess.Read, FileShare.None);
            }
            catch (IOException)
            {
                // The file is unavailable because it is still being written to
                // or being processed by another thread or does not exist (has already been processed)
                return true;
            }
            finally
            {
                if (stream != null) stream.Close();
            }
            // File is not locked
            return false;
        }
        public static bool WaitForFileToUnlock(string fileName, long miliseconds)
        {
            DateTime start = DateTime.Now;
            while (IsFileLocked(fileName))
            {
                if ((DateTime.Now - start).TotalMilliseconds >= miliseconds) return false;
                System.Threading.Thread.Sleep(100);
            }
            return true;
        }
        public static string[] ReadAllLines(string fileName, bool trimStart = false)
        {
            List<string> lines = new List<string>(100000);
            //
            if (!WaitForFileToUnlock(fileName, 5000)) return null;
            //
            using (FileStream fileStream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (StreamReader streamReader = new StreamReader(fileStream, Encoding.UTF8, true, 16*4096))
            {
                fileStream.Position = 0;
                while (!streamReader.EndOfStream)
                {
                    if (trimStart) lines.Add(streamReader.ReadLine().TrimStart());
                    else lines.Add(streamReader.ReadLine());
                }
                //
                streamReader.Close();
                fileStream.Close();
            }
            //
            return lines.ToArray();
        }

        public static List<string> GetLockingProcesses(string filePath)
        {
            List<string> result = new List<string>();
            //
            if (!File.Exists(filePath)) return result;
            //
            if (CanOpenFile(filePath))
            {
                return result; // No process is locking the file
            }
            //
            foreach (var process in Process.GetProcesses())
            {
                try
                {
                    if (process.Id == 30960)
                        result = result;

                    if (process.ProcessName.ToLower().StartsWith("ccx"))
                    {

                        List<string> openFiles = GetOpenFiles(process.Id).ToList();

                        foreach (var file in openFiles)
                        {
                            if (string.Equals(file, filePath, StringComparison.OrdinalIgnoreCase))
                            {
                                result.Add($"{process.ProcessName} (PID: {process.Id})");
                            }
                        }
                    }
                }
                catch
                {
                    // Ignore access denied errors
                }
            }
            //
            return result;
        }
        private static bool CanOpenFile(string filePath)
        {
            try
            {
                using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.None))
                {
                    return true;
                }
            }
            catch
            {
                return false; // File is locked
            }
        }
        private static IEnumerable<string> GetOpenFiles(int processId)
        {
            var files = new List<string>();
            //
            try
            {
                string query = $"SELECT ProcessId, Name FROM Win32_Process WHERE ProcessId = {processId}";
                using (var searcher = new System.Management.ManagementObjectSearcher("root\\CIMV2", query))
                {
                    foreach (System.Management.ManagementObject obj in searcher.Get())
                    {
                        files.Add(obj["Name"].ToString());
                    }
                }
            }
            catch
            {
                // Ignore errors
            }
            //
            return files;
        }

        // Read number of lines        
        [DebuggerStepThrough]
        public static bool IsNullOrEmptyOrWhiteSpace(this string value)
        {
            return string.IsNullOrWhiteSpace(value);
        }
        [DebuggerStepThrough]
        public static T NotNull<T>(T value, string argName) where T : class
        {
            if (argName.IsNullOrEmptyOrWhiteSpace()) { argName = "Invalid"; }
            //
            if (value == null) throw new Exception(argName);
            return value;
        }
        [DebuggerStepThrough]
        public static long CountLines(this Stream stream, Encoding encoding = default)
        {
            const char CR = '\r';
            const char LF = '\n';
            const char NULL = (char)0;
            //
            NotNull(stream, nameof(stream));
            //
            var lineCount = 0L;
            var byteBuffer = new byte[1024 * 1024];
            var detectedEOL = NULL;
            var currentChar = NULL;
            int bytesRead;
            //
            if (encoding is null || Equals(encoding, Encoding.ASCII) || Equals(encoding, Encoding.UTF8))
            {
                while ((bytesRead = stream.Read(byteBuffer, 0, byteBuffer.Length)) > 0)
                {
                    for (var i = 0; i < bytesRead; i++)
                    {
                        currentChar = (char)byteBuffer[i];
                        //
                        if (detectedEOL != NULL)
                        {
                            if (currentChar == detectedEOL)
                            {
                                lineCount++;
                            }
                        }
                        else if (currentChar == LF || currentChar == CR)
                        {
                            detectedEOL = currentChar;
                            lineCount++;
                        }
                    }
                }
            }
            else
            {
                var charBuffer = new char[byteBuffer.Length];
                //
                while ((bytesRead = stream.Read(byteBuffer, 0, byteBuffer.Length)) > 0)
                {
                    var charCount = encoding.GetChars(byteBuffer, 0, bytesRead, charBuffer, 0);
                    //
                    for (var i = 0; i < charCount; i++)
                    {
                        currentChar = charBuffer[i];
                        //
                        if (detectedEOL != NULL)
                        {
                            if (currentChar == detectedEOL)
                            {
                                lineCount++;
                            }
                        }
                        else if (currentChar == LF || currentChar == CR)
                        {
                            detectedEOL = currentChar;
                            lineCount++;
                        }
                    }
                }
            }
            //
            if (currentChar != LF && currentChar != CR && currentChar != NULL)
            {
                lineCount++;
            }
            //
            return lineCount;
        }
        // String
        public static string GetRandomString(int len)
        {
            var chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            var stringChars = new char[len];
            var random = new Random();
            for (int i = 0; i < stringChars.Length; i++)
            {
                stringChars[i] = chars[random.Next(chars.Length)];
            }
            return new String(stringChars);
        }
        // Sort
        public static void Sort3_descending(ref float arr0, ref float arr1, ref float arr2)
        {
            float arr120;
            float arr121;

            // sort first two values
            if (arr0 > arr1)
            {
                arr120 = arr0;
                arr121 = arr1;
            } // if
            else
            {
                arr120 = arr1;
                arr121 = arr0;
            } // else

            // decide where to put arr12 and the third original value arr[ 3 ]
            if (arr121 > arr2)
            {
                arr0 = arr120;
                arr1 = arr121;
            } // if
            else if (arr2 > arr120)
            {
                arr0 = arr2;
                arr1 = arr120;
                arr2 = arr121;
            } // if
            else
            {
                arr0 = arr120;
                arr1 = arr2;
                arr2 = arr121;
            } // else
        }
        public static void Sort3_descending(ref double arr0, ref double arr1, ref double arr2)
        {
            double arr120;
            double arr121;

            // sort first two values
            if (arr0 > arr1)
            {
                arr120 = arr0;
                arr121 = arr1;
            } // if
            else
            {
                arr120 = arr1;
                arr121 = arr0;
            } // else

            // decide where to put arr12 and the third original value arr[ 3 ]
            if (arr121 > arr2)
            {
                arr0 = arr120;
                arr1 = arr121;
            } // if
            else if (arr2 > arr120)
            {
                arr0 = arr2;
                arr1 = arr120;
                arr2 = arr121;
            } // if
            else
            {
                arr0 = arr120;
                arr1 = arr2;
                arr2 = arr121;
            } // else
        }
        // Solve Qubic
        public static void SolveQubicEquationDepressedCubic(double a, double b, double c, double d,
                                                            ref double x1, ref double x2, ref double x3)
        {
            // https://en.wikipedia.org/wiki/Cubic_function
            // https://www.easycalculation.com/algebra/cubic-equation.php - check source
            double p;
            double q;
            double tmp1;
            double tmp2;
            double alpha;

            p = (3.0 * a * c - b * b) / (3.0 * a * a);
            if (p > 0)
            {
                // One root real, two are complex
                x1 = x2 = x3 = 0;
            }
            else if (p == 0)
            {
                // All roots real, at least two are equal.
                double term1 = b * _oneThird;
                double r = -(27f * d) + b * (9f * c - 2f * b * b);
                double r13 = (r < 0) ? -Math.Pow(-r, _oneThird) : Math.Pow(r, _oneThird);
                x1 = -term1 + 2f * r13;
                x2 = -(r13 + term1);
                x3 = x2;
            }
            else
            {
                q = (2.0 * b * b * b - 9.0 * a * b * c + 27.0 * a * a * d) / (27.0 * a * a * a);
                //
                tmp1 = (3.0 * q) / (2.0 * p) * Math.Sqrt(-3.0 / p);
                if (tmp1 > 1.0) tmp1 = 1.0;
                else if (tmp1 < -1.0) tmp1 = -1.0;
                alpha = 1.0 / 3.0 * Math.Acos(tmp1);
                //
                tmp1 = 2.0 * Math.Sqrt(-p / 3.0);
                tmp2 = b / (3.0 * a);
                x1 = tmp1 * Math.Cos(alpha) - tmp2;
                x2 = tmp1 * Math.Cos(alpha - 2.0 * Math.PI / 3.0) - tmp2;
                x3 = tmp1 * Math.Cos(alpha - 4.0 * Math.PI / 3.0) - tmp2;
            }
        }
        public static void SolveQubicEquationDepressedCubicF(float a, float b, float c, float d,
                                                             ref float x1, ref float x2, ref float x3)
        {
            // https://en.wikipedia.org/wiki/Cubic_function
            // https://www.easycalculation.com/algebra/cubic-equation.php - check source
            float p;
            float q;
            float tmp1;
            float tmp2;
            float alpha;
            //
            p = (3f * a * c - b * b) / (3f * a * a);
            if (p > 0)
            {
                // One root real, two are complex
                x1 = x2 = x3 = 0;
            }
            else if (p == 0)
            {
                // All roots real, at least two are equal.
                float term1 = b * _oneThird;
                float r = -(27f * d) + b * (9f * c - 2f * (b * b));
                float r13 = (r < 0) ? (float)(-Math.Pow(-r, _oneThird)) : (float)Math.Pow(r, _oneThird);
                x1 = -term1 + 2f * r13;
                x2 = -(r13 + term1);
                x3 = x2;
            }
            else
            {
                q = (2f * b * b * b - 9f * a * b * c + 27f * a * a * d) / (27f * a * a * a);
                //
                tmp1 = (3f * q) / (2f * p) * (float)Math.Sqrt(-3f / p);
                if (tmp1 > 1f) tmp1 = 1f;
                else if (tmp1 < -1f) tmp1 = -1f;
                alpha = _oneThird * (float)Math.Acos(tmp1);
                //
                tmp1 = 2f * (float)Math.Sqrt(-p / 3f);
                tmp2 = b / (3f * a);
                x1 = tmp1 * (float)Math.Cos(alpha) - tmp2;
                x2 = tmp1 * (float)Math.Cos(alpha - _twoPiThirds) - tmp2;
                x3 = tmp1 * (float)Math.Cos(alpha - _fourPiThirds) - tmp2;
            }
        }
        public static void GetPrincipalValuesFromMatrix(double s11, double s22, double s33,
                                                        double s12, double s23, double s31,
                                                        ref float x1f, ref float x2f, ref float x3f)
        {
            // https://en.wikipedia.org/wiki/Cubic_function
            // https://www.easycalculation.com/algebra/cubic-equation.php - check source
            double x1 = 0;
            double x2 = 0;
            double x3 = 0;
            //
            double I1 = s11 + s22 + s33;
            double I2 = s11 * s22 + s22 * s33 + s33 * s11 - s12 * s12 - s23 * s23 - s31 * s31;
            double I3 = s11 * s22 * s33 - s11 * s23 * s23 - s22 * s31 * s31 - s33 * s12 * s12 + 2.0 * s12 * s23 * s31;
            //
            SolveQubicEquationDepressedCubic(1, -I1, I2, -I3, ref x1, ref x2, ref x3);
            //
            x1f = (float)x1;
            x2f = (float)x2;
            x3f = (float)x3;
        }
        public static float Sin(float x) //x in radians
        {
            float sinn;
            if (x < -3.14159265f)
                x += 6.28318531f;
            else
            if (x > 3.14159265f)
                x -= 6.28318531f;

            if (x < 0)
            {
                sinn = 1.27323954f * x + 0.405284735f * x * x;

                if (sinn < 0)
                    sinn = 0.225f * (sinn * -sinn - sinn) + sinn;
                else
                    sinn = 0.225f * (sinn * sinn - sinn) + sinn;
                return sinn;
            }
            else
            {
                sinn = 1.27323954f * x - 0.405284735f * x * x;

                if (sinn < 0)
                    sinn = 0.225f * (sinn * -sinn - sinn) + sinn;
                else
                    sinn = 0.225f * (sinn * sinn - sinn) + sinn;
                return sinn;

            }
        }
        public static float Cos(float x) //x in radians
        {
            return Sin(x + 1.5707963f);
        }
        // Complex
        public static double GetPhase360(double phase)
        {
            return (phase % 360 + 360) % 360;
        }
        public static float GetComplexMagnitude(float real, float imaginary)
        {
            return (float)Math.Sqrt(real * real + imaginary * imaginary);
        }
        public static double GetComplexMagnitude(double real, double imaginary)
        {
            return Math.Sqrt(real * real + imaginary * imaginary);
        }
        public static float GetComplexPhaseDeg(float real, float imaginary)
        {
            float result;
            if (real == 0)
            {
                if (imaginary > 0) result = 90;
                else if ((imaginary < 0)) result = 270;
                else result = 0;
            }
            else
            {
                result = (float)Math.Atan(imaginary / real) * _radToDeg;
                if (real < 0) result += 180;
            }
            return result;
        }
        public static double GetComplexPhaseDeg(double real, double imaginary)
        {
            double result;
            if (real == 0)
            {
                if (imaginary > 0) result = 90;
                else if ((imaginary < 0)) result = 270;
                else result = 0;
            }
            else
            {
                result = Math.Atan(imaginary / real) * _radToDeg;
                if (real < 0) result += 180;
            }
            return result;
        }
        public static float GetComplexRealAtAngle(float real, float imaginary, float angleDeg)
        {
            float magnitude = GetComplexMagnitude(real, imaginary);
            float phaseDeg = GetComplexPhaseDeg(real, imaginary);
            return magnitude * (float)Math.Cos((phaseDeg + angleDeg) * _degToRad);
        }
        public static double GetComplexRealAtAngle(double real, double imaginary, double angleDeg)
        {
            double magnitude = GetComplexMagnitude(real, imaginary);
            double phaseDeg = GetComplexPhaseDeg(real, imaginary);
            return magnitude * Math.Cos((phaseDeg + angleDeg) * _degToRad);
        }
        public static float GetComplexRealAtAngleFromMagAndPha(float magnitude, float phaseDeg, float angleDeg)
        {
            return magnitude * (float)Math.Cos((phaseDeg + angleDeg) * _degToRad);
        }
        //
        public static int[] GetSortedKey(int id1, int id2)
        {
            if (id1 < id2) return new int[] { id1, id2 };
            else return new int[] { id2, id1 };
        }
        public static int GetHashCode(int[] array)
        {
            int hash = 23;
            for (int i = 0; i < array.Length; i++)
            {
                hash = hash * 31 + array[i].GetHashCode();
            }
            return hash;
        }
        public static int GetHashCode(int[][] array)
        {
            int hash = 23;
            for (int i = 0; i < array.Length; i++)
            {
                hash = hash * 31 + GetHashCode(array[i]);
            }
            return hash;
        }
    }
}
