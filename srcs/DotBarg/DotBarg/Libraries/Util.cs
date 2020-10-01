using DotBarg.Libraries.Roslyns;
using DotBarg.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;

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

        #region Roslyn 系: 指定位置の定義元を検索して、定義位置情報を返却


        // Util の名前空間に Roslyn 系を書きたくなかったから RoslynHelper 経由にしたが、余計だったか？

        public async static Task<SearchResultInfo> FindSymbolAtPositionAsync(string sourceFile, int offset)
        {
            return await RoslynHelper.FindSymbolAtPositionAsync(sourceFile, offset);
        }


        #endregion

        #region MainViewModel に依存する系


        // この MainViewModel は局所的にしか使いません。基本的には触らないでください。
        // 以下のほか、拡張コントロール内でフォントサイズ変更用に MainVM をバインドしています。

        public static MainViewModel MainVM;


        #region ステータスバーの通知


        public static void SetStatusBarMessage(string value, bool autoClear = false)
        {
            MainVM.StatusBarMessage = value;
            DoEvents();

            if (autoClear)
            {
                // ※この処理を await で待機すると、（応答はあるが）3秒間待たされるので、わざと待機しない
                Task.Run(async () =>
                {
                    await Task.Delay(3000);
                    MainVM.StatusBarMessage = string.Empty;
                });
            }
        }


        // 現在メッセージ待ち行列の中にある全てのUIメッセージを処理します。
        // https://gist.github.com/pinzolo/2814091

        public static void DoEvents()
        {
            var frame = new DispatcherFrame();
            var callback = new DispatcherOperationCallback(obj =>
            {
                (obj as DispatcherFrame).Continue = false;
                return null;
            });

            Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background, callback, frame);
            Dispatcher.PushFrame(frame);
        }


        #endregion


        #endregion



    }
}
