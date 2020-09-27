﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace DotBarg.Libraries
{
    public class Util
    {
        #region 相対パスから絶対パスへの変換


        /// <summary>
        /// 起点フォルダをもとに、相対パスのファイルの絶対パスを取得します。
        /// </summary>
        /// <param name="baseFolder">起点となる絶対パスのフォルダパス</param>
        /// <param name="relativeFile">絶対パスを取得したい、相対パスのファイルパス</param>
        /// <returns>絶対パスのファイルパス</returns>
        public static string GetFullPath(string baseFolder, string relativeFile)
        {
            if (!baseFolder.EndsWith(@"\"))
                baseFolder += @"\";

            var uri1 = new Uri(baseFolder);
            var uri2 = new Uri(uri1, relativeFile);

            return uri2.LocalPath;
        }


        #endregion

        #region テキストファイルの読み込み


        /// <summary>
        /// 指定ファイルのエンコードを取得します。
        /// </summary>
        /// <param name="filePath">エンコードを取得したいファイル</param>
        /// <returns>ファイルのエンコード</returns>
        public static Encoding GetEncoding(string filePath)
        {
            return EncodeResolver.GetEncoding(filePath);
        }

        /// <summary>
        /// ファイルの内容を読み込みます。
        /// </summary>
        /// <param name="filePath">読み込むファイル</param>
        /// <returns></returns>
        public static async Task<string> ReadToEndAsync(string filePath)
        {
            return await ReadToEndAsync(filePath, GetEncoding(filePath));
        }

        /// <summary>
        /// ファイルの内容を読み込みます。
        /// </summary>
        /// <param name="filePath">読み込むファイル</param>
        /// <param name="encoding">エンコード</param>
        /// <returns></returns>
        public static async Task<string> ReadToEndAsync(string filePath, Encoding encoding)
        {
            using (var stream = File.OpenRead(filePath))
            using (var reader = new StreamReader(stream, encoding))
            {
                return await reader.ReadToEndAsync();
            }
        }

        /// <summary>
        /// ファイルの内容を読み込みます。
        /// </summary>
        /// <param name="filePath">読み込むファイル</param>
        /// <returns></returns>
        public static IEnumerable<string> ReadLines(string filePath)
        {
            return ReadLines(filePath, GetEncoding(filePath));
        }

        //public static IEnumerable<string> ReadLines(string filePath, Encoding encoding)
        //{
        //    return File.ReadLines(filePath, encoding);
        //}

        /// <summary>
        /// ファイルの内容を読み込みます。
        /// </summary>
        /// <param name="filePath">読み込むファイル</param>
        /// <param name="encoding">エンコード</param>
        /// <returns></returns>
        public static IEnumerable<string> ReadLines(string filePath, Encoding encoding)
        {
            using (var stream = File.OpenRead(filePath))
            using (var reader = new StreamReader(stream, encoding))
            {
                var line = Task.Run(async () => await ReadLineAsync(reader)).Result;
                
                if (line is null)
                    yield break;
                else
                    yield return line;
            }
        }

        /// <summary>
        /// ファイルの内容のうち、１行分を読み込みます。
        /// </summary>
        /// <param name="reader">StreamReader</param>
        /// <returns></returns>
        public static async Task<string> ReadLineAsync(StreamReader reader)
        {
            return await reader.ReadLineAsync();
        }


        #endregion

        #region テキストファイルの書き込み


        /// <summary>
        /// ファイルに内容を書き込みます。
        /// </summary>
        /// <param name="filePath">書き込むファイル</param>
        /// <param name="content">内容</param>
        /// <param name="append">true: 追記, false: 上書き（既存内容は消えます）</param>
        /// <returns></returns>
        public static async Task WriteAsync(string filePath, string content, bool append = false)
        {
            await WriteAsync(filePath, content, append, GetEncoding(filePath));
        }

        /// <summary>
        /// ファイルに内容を書き込みます。
        /// </summary>
        /// <param name="filePath">書き込むファイル</param>
        /// <param name="content">内容</param>
        /// <param name="append">true: 追記, false: 上書き（既存内容は消えます）</param>
        /// <param name="encoding">エンコード</param>
        /// <returns></returns>
        public static async Task WriteAsync(string filePath, string content, bool append, Encoding encoding)
        {
            using (var writer = new StreamWriter(filePath, append, encoding))
            {
                await writer.WriteAsync(content);
            }
        }

        /// <summary>
        /// ファイルに内容を書き込みます。書き込む際は、最後に改行を追記します。
        /// </summary>
        /// <param name="filePath">書き込むファイル</param>
        /// <param name="content">内容</param>
        /// <param name="append">true: 追記, false: 上書き（既存内容は消えます）</param>
        /// <returns></returns>
        public static async Task WriteLineAsync(string filePath, string content, bool append = false)
        {
            await WriteLineAsync(filePath, content, append, GetEncoding(filePath));
        }

        /// <summary>
        /// ファイルに内容を書き込みます。書き込む際は、最後に改行を追記します。
        /// </summary>
        /// <param name="filePath">書き込むファイル</param>
        /// <param name="content">内容</param>
        /// <param name="append">true: 追記, false: 上書き（既存内容は消えます）</param>
        /// <param name="encoding">エンコード</param>
        /// <returns></returns>
        public static async Task WriteLineAsync(string filePath, string content, bool append, Encoding encoding)
        {
            using (var writer = new StreamWriter(filePath, append, encoding))
            {
                await writer.WriteLineAsync(content);
            }
        }


        #endregion

        #region object 版の null チェック


        public static bool IsNull(object obj)
        {
            return (obj is null);
        }

        public static bool IsNotNull(object obj)
        {
            return !IsNull(obj);
        }


        #endregion

    }
}