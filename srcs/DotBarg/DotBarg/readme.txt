■NuGet パッケージ
（2020/09 時点）


・Roslyn 関係（ソースコードのパースと、ソリューションファイル・プロジェクトファイルを扱うもの）
Microsoft.CodeAnalysis.CSharp
Microsoft.CodeAnalysis.VisualBasic
Microsoft.CodeAnalysis.Workspaces.MSBuild
Microsoft.CodeAnalysis.CSharp.Workspaces
Microsoft.CodeAnalysis.VisualBasic.Workspaces

・AvalonDock
Extended.Wpf.Toolkit

・AvalonEdit
AvalonEdit

・Livet
LivetCask
LivetExtensions

・テキストファイルのエンコードの自動判定
ReadJEnc


※パッケージ名の変更
2018/10 時点では、
Microsoft.CodeAnalysis.Workspaces.MSBuild　ではなく、
Microsoft.MSBuild　という名前でした。

今後も、名称変更の影響があるのかもしれません。



■独自アイコン

exe ファイルに対するアイコン設定
→プロジェクト設定画面より app.ico をセット

各 Window に対するアイコン設定
→各 Window にセットするのは手間なので一元管理する
　→ app.xaml/Resources 内で Window のスタイルにてセット

