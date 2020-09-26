using Livet;
using System;
using System.Threading.Tasks;

namespace DotBarg.ViewModels
{
    public class ProgressViewModel : ViewModel
    {
        public Func<Task> DoWorkAsync { get; set; }

        public async void Initialize()
        {
            // 受け取った処理を実行
            await Task.Run(async() => await DoWorkAsync());
            //await DoWorkAsync();

            // 処理は終わっているが、なかなかツリー表示されない現象の仮対応。ツリー表示されてから進捗画面を閉じるようにする
            //（デバッグで出力ペインを見ると、Roslyn 関係のアセンブリファイルを読み込んでいる？）
            // 5 秒待機は妥当ではないかもしれない（プロジェクト数や環境の違いによって）
            //await Task.Delay(5000);

            // 処理が終わったら、自動で画面終了
            await this.CloseAsync();
        }
    }
}
