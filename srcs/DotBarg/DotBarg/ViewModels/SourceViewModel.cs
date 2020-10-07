using DotBarg.Libraries;
using DotBarg.Libraries.DBs;
using DotBarg.Models;
using Livet;
using Livet.Commands;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows.Data;

namespace DotBarg.ViewModels
{
    public class SourceViewModel : DocumentPaneViewModel
    {
        #region フィールド・プロパティ


        // 設計方針的に SourceViewModel のコレクションを管理するのが MainViewModel なので、ここでは参照用です。
        // 基本的には何もしないでください。
        public MainViewModel MainVM { get; set; }


        #endregion

        #region 変更通知プロパティ


        // AvalonDock 関連

        public override string Title
        {
            get { return Path.GetFileName(SourceFile); }
        }

        public override string ContentId
        {
            get { return SourceFile; }
        }

        public override TreeNodeKinds TreeNodeKinds
        {
            get
            {
                var value = TreeNodeKinds.None;
                var extension = Path.GetExtension(SourceFile).ToLower();

                if (extension == ".cs")
                    value = TreeNodeKinds.CSharpSourceFile;

                if (extension == ".vb")
                    value = TreeNodeKinds.VBNetSourceFile;

                return value;
            }
        }


        // AvalonEdit 関連

        public string HeaderTitle
        {
            get { return GetRelativeFilePath(SourceFile); }
        }

        private string GetRelativeFilePath(string sourceFile)
        {
            var prjInfo = AppEnv.ProjectInfos.FirstOrDefault(x =>
            {
                if (x.SourceFiles.Any(y => y.SourceFile == sourceFile))
                    return true;
                else
                    return false;
            });

            var srcFolder = Path.GetDirectoryName(sourceFile);
            var prjFolder = Path.GetDirectoryName(prjInfo.ProjectFile);
            var difFolder = srcFolder.Replace(prjFolder, string.Empty);
            var result = string.Empty;

            if (string.IsNullOrEmpty(difFolder))
            {
                // プロジェクトフォルダ名/ソースファイル名
                var fi = new FileInfo(sourceFile);
                result = $"{fi.Directory.Name}/{fi.Name}";
            }
            else
            {
                difFolder = difFolder.Replace(@"\", "/");

                if (difFolder.StartsWith("/"))
                    difFolder = difFolder.Substring(1);

                // プロジェクトフォルダ名/サブフォルダ名/.../ソースファイル名
                var di = new DirectoryInfo(prjFolder);
                var fi = new FileInfo(sourceFile);

                prjFolder = di.Name;
                result = $"{prjFolder}/{difFolder}/{fi.Name}";
            }

            result = result.Replace("/", " / ");

            return result;
        }

        private string _SourceFile;
        public string SourceFile
        {
            get { return _SourceFile; }
            set { RaisePropertyChangedIfSet(ref _SourceFile, value); }
        }

        private string _SourceCode;
        public string SourceCode
        {
            get { return _SourceCode; }
            set { RaisePropertyChangedIfSet(ref _SourceCode, value); }
        }

        // キャレットの文字数位置。ソースコードを１つの String として見た際、何文字目か

        private int _CaretOffset;
        public int CaretOffset
        {
            get { return _CaretOffset; }
            set { RaisePropertyChangedIfSet(ref _CaretOffset, value); }
        }

        // View 関連

        // ソースツリー

        private ObservableCollection<TreeViewItemModel> _SourceTreeItems;
        public ObservableCollection<TreeViewItemModel> SourceTreeItems
        {
            get { return _SourceTreeItems; }
            set { RaisePropertyChangedIfSet(ref _SourceTreeItems, value); }
        }

        // メンバーツリー

        private ObservableCollection<TreeViewItemModel> _MemberTreeItems;
        public ObservableCollection<TreeViewItemModel> MemberTreeItems
        {
            get { return _MemberTreeItems; }
            set { RaisePropertyChangedIfSet(ref _MemberTreeItems, value); }
        }

        // 継承元ツリー

        private ObservableCollection<DefinitionItemModel> _InheritanceSourceTreeItems;
        public ObservableCollection<DefinitionItemModel> InheritanceSourceTreeItems
        {
            get { return _InheritanceSourceTreeItems; }
            set { RaisePropertyChangedIfSet(ref _InheritanceSourceTreeItems, value); }
        }

        // 継承先ツリー

        private ObservableCollection<DefinitionItemModel> _InheritanceDestinationTreeItems;
        public ObservableCollection<DefinitionItemModel> InheritanceDestinationTreeItems
        {
            get { return _InheritanceDestinationTreeItems; }
            set { RaisePropertyChangedIfSet(ref _InheritanceDestinationTreeItems, value); }
        }


        #endregion

        #region コマンド



        // 不具合
        // LivetCallMethodAction 経由（メソッド直接バインディング）だと、NullReferenceException が発生してしまう

        // 現象再現手順
        // エディタ内、クラス範囲内の任意の位置をクリックしてキャレット位置を更新 → その後クラスメンバーツリーで任意のメソッドノードをクリック → その後エディタ内をクリックすると、NullReferenceException

        // Livet.dll 内
        // System.NullReferenceException はハンドルされませんでした。
        // HResult=-2147467261
        // Message=オブジェクト参照がオブジェクト インスタンスに設定されていません。
        // 場所 Livet.Behaviors.MethodBinderWithArgument.Invoke(Object targetObject, String methodName, Object argument)

        // 例外エラーの発生場所が、TreeItems のセッター内、RaisePropertyChanged であり、ソースを確認したが分からず。
        // https://github.com/ugaya40/Livet/blob/master/.NET4.0/Livet(.NET4.0)/Behaviors/MethodBinderWithArgument.cs

        // 対策１
        // ObservableCollection 内のデータの扱い方について、都度インスタンス生成し直しする扱い方がまずいのかと思い、
        // Clear メソッドと Add メソッドで書き換えてみたが、それでも現象が出た

        // 対策２
        // メソッド直接バインディングを止めて、コマンドバインディングに変更することで回避可能だった（MethodBinderWithArgument.Invoke メソッドを通らない実行経路に変えた）
        // WPF: TreeViewItem bound to an ICommand
        // https://stackoverflow.com/questions/2266890/wpf-treeviewitem-bound-to-an-icommand



        //Public Sub MemberTree_SelectedItemChanged(e As TreeViewItemModel)
        //    Console.WriteLine(e)
        //End Sub



        // ソースツリーノードのクリック

        private ListenerCommand<TreeViewItemModel> _SourceTreeSelectedItemChangedCommand;
        public ListenerCommand<TreeViewItemModel> SourceTreeSelectedItemChangedCommand
        {
            get { return this.SetCommand(ref _SourceTreeSelectedItemChangedCommand, SourceTreeSelectedItemChanged); }
        }

        private void SourceTreeSelectedItemChanged(TreeViewItemModel e)
        {
            if (e is null || e.FileName != SourceFile)
                return;

            // 通常はキャレット位置が変わったら、TextEditorEx_CaretPositionChanged イベントハンドラが実行されていいのですが、
            // 各ツリーノードのクリック時は、結果的に無駄処理として実行されるだけなので、フラグを立ててイベントハンドラが動作しないように抑制します。
            // ただし、ソースツリーで選択した定義が、メンバーツリー（や継承ツリー）で表示中の定義範囲外の場合、表示更新したいため、わざと抑制フラグは立てません。
            // ※ NowWorking フラグは、TextEditorEx_CaretPositionChanged() 内で使用しているイベント処理の抑制フラグです。
            var useEventSkipFlag = false;
            
            if (MemberTreeItems.Any())
            {
                var item = MemberTreeItems.FirstOrDefault();
                if (item.StartLength <= e.StartLength && e.EndLength <= item.EndLength)
                    useEventSkipFlag = true;

                // 例外として、インナーのクラス、構造体、インターフェースの場合は再表示させる
                switch (e.TreeNodeKinds)
                {
                    case TreeNodeKinds.Class:
                    case TreeNodeKinds.Struct:
                    case TreeNodeKinds.Interface:

                        useEventSkipFlag = false;
                        break;
                }
            }

            if (useEventSkipFlag)
                NowWorking = true;

            CaretOffset = e.StartLength;

            if (useEventSkipFlag)
                NowWorking = false;
        }


        // メンバーツリーノードのクリック

        private ListenerCommand<TreeViewItemModel> _MemberTreeSelectedItemChangedCommand;
        public ListenerCommand<TreeViewItemModel> MemberTreeSelectedItemChangedCommand
        {
            get { return this.SetCommand(ref _MemberTreeSelectedItemChangedCommand, MemberTreeSelectedItemChanged); }
        }

        private void MemberTreeSelectedItemChanged(TreeViewItemModel e)
        {
            SourceTreeSelectedItemChanged(e);
        }


        #endregion

        #region コンストラクタ


        public SourceViewModel()
        {
            CanClose = true;

            SourceFile = string.Empty;
            SourceCode = string.Empty;
            CaretOffset = 0;

            SourceTreeItems = new ObservableCollection<TreeViewItemModel>();
            MemberTreeItems = new ObservableCollection<TreeViewItemModel>();
            InheritanceSourceTreeItems = new ObservableCollection<DefinitionItemModel>();
            InheritanceDestinationTreeItems = new ObservableCollection<DefinitionItemModel>();

            BindingOperations.EnableCollectionSynchronization(SourceTreeItems, new object());
            BindingOperations.EnableCollectionSynchronization(MemberTreeItems, new object());
            BindingOperations.EnableCollectionSynchronization(InheritanceSourceTreeItems, new object());
            BindingOperations.EnableCollectionSynchronization(InheritanceDestinationTreeItems, new object());
        }


        #endregion

        #region エディタ内にあるキャレットカーソル位置の移動イベント


        private Languages ThisLanguages = Languages.Unknown;

        private Languages GetLanguages()
        {
            var result = default(Languages);
            var ext = Path.GetExtension(SourceFile).ToLower();

            switch (ext)
            {
                case ".cs":
                    result = Languages.CSharp;
                    break;

                case ".vb":
                    result = Languages.VBNet;
                    break;
            }

            return result;
        }


        private bool NowWorking = false;
        public void TextEditorEx_CaretPositionChanged()
        {
            // 処理が終わる前に、次のイベントが発生した場合は飛ばす
            if (NowWorking)
                return;

            NowWorking = true;

            // キャレット位置の範囲に該当するクラスやメソッドが無いか、メモリDBを見ながら探す

            // 個別に非同期で実行するか迷ったが止めた
            //
            // 非同期にするなら、UserDefinitions をコピーして各メソッドに渡すことになるが、
            // List.ToList() は、新しいインスタンスを返すが、中身の要素自体は、コピー元と同一なので、
            // 別途 UserDefinition, BaseTypeInfo, MethodArgument それぞれに Copy() を追加して呼び出さないといけない（完全分離コピーするためには）
            //
            // そこまで速さは求めていないため、同期処理にした

            if (!AppEnv.UserDefinitions.Any(x => x.SourceFile == SourceFile))
                return;

            // ステータスバーへ通知
            Util.SetStatusBarMessage("補助情報を取得中 ...");

            // このソースコードの開発言語を設定
            if (ThisLanguages == Languages.Unknown)
                ThisLanguages = GetLanguages();

            AppEnv.Languages = ThisLanguages;

            // ソースツリー
            ShowSourceTree();

            // メンバーツリー
            ShowMemberTree();

            // 継承元ツリー
            ShowInheritanceSourceTree();

            // 継承先ツリー
            ShowInheritanceDestinationTree();

            // ステータスバーへ通知
            Util.SetStatusBarMessage("補助情報の取得完了", true);

            NowWorking = false;
        }

        // ソースツリー

        private void ShowSourceTree()
        {
            // 一度設定したら何もしない
            if (SourceTreeItems.Any())
                return;

            var root = AppEnv.UserDefinitions
                .Where(x => x.SourceFile == SourceFile && x.DefineKinds == DefineKinds.Namespace)
                .FirstOrDefault();

            if (root is null)
                return;

            // UserDefinitions テーブル的には平坦化されて管理しているが、
            // ソースコード上では、クラスとそのインタークラスがあった場合、階層関係がある
            // 階層的にツリー表示したいため、名前空間を追跡しながらツリー作成していく
            // 
            // namespace, class, struct, interface, module, enum, delegate, event
            var rows = AppEnv.UserDefinitions
                .Where(x => x.SourceFile == SourceFile && x.Namespace == root.DefineFullName)
                .ToList();

            var item = default(TreeViewItemModel);

            if (rows is null || !rows.Any())
                return;

            foreach (var row in rows)
            {
                switch (row.DefineKinds)
                {
                    case DefineKinds.Namespace:
                        break;

                    case DefineKinds.Class:
                    case DefineKinds.Struct:
                    case DefineKinds.Interface:
                    case DefineKinds.Module:

                        item = ConvertTreeViewItemModel(row);
                        SourceTreeItems.Add(item);

                        var parentNamespace = row.DefineFullName;
                        AddMember(item, parentNamespace);

                        if (item.Children.Any())
                            item.IsExpanded = true;

                        break;

                    default:

                        item = ConvertTreeViewItemModel(row);
                        SourceTreeItems.Add(item);
                        break;
                }
            }
        }

        private void AddMember(TreeViewItemModel parent, string parentNamespace)
        {
            // インナークラスなど、クラス内で定義したクラス等を除くものを取得
            // namespace, class, struct, interface, module, enum, delegate, event, field, property, method(Operator, WindowsAPI, EventHandler, method)

            // インナークラスのメンバーが、親クラスのメンバーとして登録されてしまうバグの対応
            // ループ中、再帰ループから戻ってきた際、rows がおかしくなる挙動のため（遅延評価の影響か）、
            // IEnumerable のままから List に変換して、コレクションを確定してからループするように修正

            // partial クラスのメンバー分が表示されないバグ、継承元クラスのメンバーが表示されないバグの対応
            // Where 句内の SourceFile フィルタを外した
            var rows = AppEnv.UserDefinitions
                .Where(x => x.Namespace == parentNamespace)
                .ToList();
            
            var item = default(TreeViewItemModel);

            if (rows is null || !rows.Any())
                return;

            foreach (var row in rows)
            {
                switch (row.DefineKinds)
                {
                    case DefineKinds.Namespace:
                        break;

                    case DefineKinds.Class:
                    case DefineKinds.Struct:
                    case DefineKinds.Interface:
                    case DefineKinds.Module:

                        item = ConvertTreeViewItemModel(row);
                        parent.Children.Add(item);

                        parentNamespace = row.DefineFullName;
                        AddMember(item, parentNamespace);

                        if (item.Children.Any())
                            item.IsExpanded = true;

                        break;

                    default:

                        item = ConvertTreeViewItemModel(row);
                        parent.Children.Add(item);
                        break;
                }
            }
        }


        private TreeViewItemModel ConvertTreeViewItemModel(UserDefinition item)
        {
            var model = new TreeViewItemModel
            {
                Text = item.DisplaySignature,
                TreeNodeKinds = ConvertTreeNodeKinds(item.DefineKinds),
                FileName = SourceFile,
                StartLength = item.StartLength,
                EndLength = item.EndLength,
            };

            // enum の場合、子ノードにメンバーを追加
            if (item.DefineKinds == DefineKinds.Enum && item.EnumMembers.Any())
            {
                foreach (var member in item.EnumMembers)
                {
                    var memberModel = new TreeViewItemModel
                    {
                        Text = member,
                        TreeNodeKinds = ConvertTreeNodeKinds(item.DefineKinds, true),
                        StartLength = item.StartLength,
                        EndLength = item.EndLength,
                    };

                    model.Children.Add(memberModel);
                }

                model.IsExpanded = true;
            }

            return model;
        }

        private TreeNodeKinds ConvertTreeNodeKinds(DefineKinds value, bool isEnumMember = false)
        {
            var result = TreeNodeKinds.None;

            switch (value)
            {
                case DefineKinds.Class: result = TreeNodeKinds.Class; break;
                case DefineKinds.Struct: result = TreeNodeKinds.Struct; break;
                case DefineKinds.Interface: result = TreeNodeKinds.Interface; break;
                case DefineKinds.Module: result = TreeNodeKinds.Module; break;
                case DefineKinds.Enum: result = TreeNodeKinds.Enum; break;

                case DefineKinds.Event: result = TreeNodeKinds.Event; break;
                case DefineKinds.Delegate: result = TreeNodeKinds.Delegate; break;
                case DefineKinds.Field: result = TreeNodeKinds.Field; break;
                case DefineKinds.Property: result = TreeNodeKinds.Property; break;
                case DefineKinds.Indexer: result = TreeNodeKinds.Property; break;

                case DefineKinds.Constructor: result = TreeNodeKinds.Method; break;
                case DefineKinds.Operator: result = TreeNodeKinds.Operator; break;
                case DefineKinds.WindowsAPI: result = TreeNodeKinds.Method; break;
                case DefineKinds.EventHandler: result = TreeNodeKinds.Method; break;
                case DefineKinds.Method: result = TreeNodeKinds.Method; break;
            }

            if (isEnumMember)
                result = TreeNodeKinds.EnumItem;

            return result;
        }

        // メンバーツリー

        private void ShowMemberTree()
        {
            var item = FindUserDefinition();
            if (item is null)
            {
                MemberTreeItems.Clear();
                return;
            }

            // class, struct, interface, module / enum, delegate, event
            var model = ConvertTreeViewItemModel(item);

            // 前回の値が入っている場合、前回の値と比較する
            // 前回と同じ場合は何もしないで終わる
            if (MemberTreeItems.Any())
            {
                var previous = MemberTreeItems.FirstOrDefault();
                if (previous.Text == model.Text && previous.StartLength == model.StartLength)
                    return;
            }

            MemberTreeItems.Clear();
            MemberTreeItems.Add(model);

            switch (item.DefineKinds)
            {
                case DefineKinds.Class:
                case DefineKinds.Struct:
                case DefineKinds.Interface:
                case DefineKinds.Module:

                    AddFilteredMember(model, item);
                    break;
            }
        }

        private UserDefinition FindUserDefinition()
        {
            // 以下のように内側のクラス等がある場合、昇順で探すと外側のクラスだと誤判定してしまう
            // 行位置 15:
            // class Class1          // 0 - 20 の範囲
            //     class InnerClass1 // 11 - 17 の範囲
            //
            // もっとも内側のコンテナを採用したいため、降順で探す
           
            // namespace, class, struct, interface, module, enum, delegate, event, field, property, method(Operator, WindowsAPI, EventHandler, method)

            var checks1 = new List<DefineKinds>
            {
                DefineKinds.Class,
                DefineKinds.Struct,
                DefineKinds.Interface,
                DefineKinds.Module,
                DefineKinds.Enum,
                DefineKinds.Delegate,
                DefineKinds.Event,
            };

            var checks2 = new List<DefineKinds>
            {
                DefineKinds.Class,
                DefineKinds.Struct,
                DefineKinds.Interface,
            };

            var items = AppEnv.UserDefinitions
                .Where(x => x.SourceFile == SourceFile && checks1.Any(y => y == x.DefineKinds))
                .OrderBy(x => x.StartLength)
                .ThenBy(x => x.EndLength)
                .ToList();

            var foundItem = default(UserDefinition);

            for (var i = items.Count - 1; i >= 0; i--)
            {
                var item = items[i];

                if (item.StartLength <= CaretOffset && CaretOffset <= item.EndLength)
                {
                    foundItem = item;

                    // 見つけた候補が、Enum, Delegate、または Event の場合、親コンテナに含まれていないかチェック
                    // 含まれている場合、その親を候補に取り替える
                    // インナーの enum, delegate, event の時は、親のメンバー扱いする
                    if (item.DefineKinds == DefineKinds.Enum || item.DefineKinds == DefineKinds.Delegate || item.DefineKinds == DefineKinds.Event)
                    {
                        var parentItem = AppEnv.UserDefinitions.FirstOrDefault(x =>
                        {
                            var b1 = (x.SourceFile == SourceFile);
                            var b2 = (x.DefineFullName == item.Namespace);
                            var b3 = checks2.Any(y => y == x.DefineKinds);
                            if (b1 && b2 && b3)
                                return true;
                            else
                                return false;
                        });

                        if (!(parentItem is null))
                            foundItem = parentItem;
                    }

                    break;
                }
            }

            return foundItem;
        }

        private void AddFilteredMember(TreeViewItemModel parentModel, UserDefinition parentItem)
        {
            // AddMember との違いは、（定義順ではなく）メンバー順に表示する点

            // 親は class, srtuct, interface, module

            // メンバーの可能性
            // enum, delegate, event, field, property, method(Constructor, Operator, WindowsAPI, EventHandler, method)

            // partial クラスのメンバー分が表示されないバグ、継承元クラスのメンバーが表示されないバグの対応
            // Where 句内の SourceFile フィルタを外した
            var items = AppEnv.UserDefinitions.Where(x => x.Namespace == parentItem.DefineFullName);

            if (items is null || !items.Any())
                return;

            // enum
            var enumItems = items.Where(x => x.DefineKinds == DefineKinds.Enum);
            if (!(enumItems is null) && enumItems.Any())
            {
                var headerModel = new TreeViewItemModel
                {
                    Text = "列挙体",
                    TreeNodeKinds = TreeNodeKinds.Enum,
                    IsExpanded = true,
                };

                parentModel.Children.Add(headerModel);

                foreach (var enumItem in enumItems.OrderBy(x => x.DisplaySignature))
                {
                    var enumModel = ConvertTreeViewItemModel(enumItem);
                    headerModel.Children.Add(enumModel);
                }
            }

            // delegate
            var delegateItems = items.Where(x => x.DefineKinds == DefineKinds.Delegate);
            if (!(delegateItems is null) && delegateItems.Any())
            {
                var headerModel = new TreeViewItemModel
                {
                    Text = "デリゲート",
                    TreeNodeKinds = TreeNodeKinds.Delegate,
                    IsExpanded = true,
                };

                parentModel.Children.Add(headerModel);

                foreach (var delegateItem in delegateItems.OrderBy(x => x.DisplaySignature))
                {
                    var delegateModel = ConvertTreeViewItemModel(delegateItem);
                    headerModel.Children.Add(delegateModel);
                }
            }

            // event
            var eventItems = items.Where(x => x.DefineKinds == DefineKinds.Event);
            if (!(eventItems is null) && eventItems.Any())
            {
                var headerModel = new TreeViewItemModel
                {
                    Text = "イベント",
                    TreeNodeKinds = TreeNodeKinds.Event,
                    IsExpanded = true,
                };

                parentModel.Children.Add(headerModel);

                foreach (var eventItem in eventItems.OrderBy(x => x.DisplaySignature))
                {
                    var eventModel = ConvertTreeViewItemModel(eventItem);
                    headerModel.Children.Add(eventModel);
                }
            }

            // field
            var fieldItems = items.Where(x => x.DefineKinds == DefineKinds.Field);
            if (!(fieldItems is null) && fieldItems.Any())
            {
                var headerModel = new TreeViewItemModel
                {
                    Text = "フィールド",
                    TreeNodeKinds = TreeNodeKinds.Field,
                    IsExpanded = true,
                };

                parentModel.Children.Add(headerModel);

                foreach (var fieldItem in fieldItems.OrderBy(x => x.DisplaySignature))
                {
                    var fieldModel = ConvertTreeViewItemModel(fieldItem);
                    headerModel.Children.Add(fieldModel);
                }
            }

            // indexer
            var indexerItems = items.Where(x => x.DefineKinds == DefineKinds.Indexer);
            if (!(indexerItems is null) && indexerItems.Any())
            {
                var headerModel = new TreeViewItemModel
                {
                    Text = "インデクサー",
                    TreeNodeKinds = TreeNodeKinds.Property,
                    IsExpanded = true,
                };

                parentModel.Children.Add(headerModel);

                foreach (var indexerItem in indexerItems.OrderBy(x => x.DisplaySignature))
                {
                    var indexerModel = ConvertTreeViewItemModel(indexerItem);
                    headerModel.Children.Add(indexerModel);
                }
            }

            // property
            var propertyItems = items.Where(x => x.DefineKinds == DefineKinds.Property);
            if (!(propertyItems is null) && propertyItems.Any())
            {
                var headerModel = new TreeViewItemModel
                {
                    Text = "プロパティ",
                    TreeNodeKinds = TreeNodeKinds.Property,
                    IsExpanded = true,
                };

                parentModel.Children.Add(headerModel);

                foreach (var propertyItem in propertyItems.OrderBy(x => x.DisplaySignature))
                {
                    var propertyModel = ConvertTreeViewItemModel(propertyItem);
                    headerModel.Children.Add(propertyModel);
                }
            }


            // Constructor
            var constructorItems = items.Where(x => x.DefineKinds == DefineKinds.Constructor);
            if (!(constructorItems is null) && constructorItems.Any())
            {
                var headerModel = new TreeViewItemModel
                {
                    Text = "コンストラクタ",
                    TreeNodeKinds = TreeNodeKinds.Method,
                    IsExpanded = true,
                };

                parentModel.Children.Add(headerModel);

                foreach (var constructorItem in constructorItems.OrderBy(x => x.DisplaySignature))
                {
                    var constructorModel = ConvertTreeViewItemModel(constructorItem);
                    headerModel.Children.Add(constructorModel);
                }
            }

            // Operator
            var operatorItems = items.Where(x => x.DefineKinds == DefineKinds.Operator);
            if (!(operatorItems is null) && operatorItems.Any())
            {
                var headerModel = new TreeViewItemModel
                {
                    Text = "オペレーター",
                    TreeNodeKinds = TreeNodeKinds.Operator,
                    IsExpanded = true,
                };

                parentModel.Children.Add(headerModel);

                foreach (var operatorItem in operatorItems.OrderBy(x => x.DisplaySignature))
                {
                    var operatorModel = ConvertTreeViewItemModel(operatorItem);
                    headerModel.Children.Add(operatorModel);
                }
            }

            // WindowsAPI
            var windowsApiItems = items.Where(x => x.DefineKinds == DefineKinds.WindowsAPI);
            if (!(windowsApiItems is null) && windowsApiItems.Any())
            {
                var headerModel = new TreeViewItemModel
                {
                    Text = "Windows API",
                    TreeNodeKinds = TreeNodeKinds.Method,
                    IsExpanded = true,
                };

                parentModel.Children.Add(headerModel);

                foreach (var windowsApiItem in windowsApiItems.OrderBy(x => x.DisplaySignature))
                {
                    var windowsApiModel = ConvertTreeViewItemModel(windowsApiItem);
                    headerModel.Children.Add(windowsApiModel);
                }
            }

            // EventHandler
            var eventHandlerItems = items.Where(x => x.DefineKinds == DefineKinds.EventHandler);
            if (!(eventHandlerItems is null) && eventHandlerItems.Any())
            {
                var headerModel = new TreeViewItemModel
                {
                    Text = "イベントハンドラ",
                    TreeNodeKinds = TreeNodeKinds.Method,
                    IsExpanded = true,
                };

                parentModel.Children.Add(headerModel);

                foreach (var eventHandlerItem in eventHandlerItems.OrderBy(x => x.DisplaySignature))
                {
                    var eventHandlerModel = ConvertTreeViewItemModel(eventHandlerItem);
                    headerModel.Children.Add(eventHandlerModel);
                }
            }

            // method
            var methodItems = items.Where(x => x.DefineKinds == DefineKinds.Method);
            if (!(methodItems is null) && methodItems.Any())
            {
                var headerModel = new TreeViewItemModel
                {
                    Text = "メソッド",
                    TreeNodeKinds = TreeNodeKinds.Method,
                    IsExpanded = true,
                };

                parentModel.Children.Add(headerModel);

                foreach (var methodItem in methodItems.OrderBy(x => x.DisplaySignature))
                {
                    var methodModel = ConvertTreeViewItemModel(methodItem);
                    headerModel.Children.Add(methodModel);
                }
            }

            if (parentModel.Children.Any())
                parentModel.IsExpanded = true;
        }

        // 継承元ツリー

        private void ShowInheritanceSourceTree()
        {
            var item = FindUserDefinition();
            if (item is null)
            {
                InheritanceSourceTreeItems.Clear();
                return;
            }

            // 継承できるもの以外は何もしないで抜ける
            switch (item.DefineKinds)
            {
                case DefineKinds.Class:
                case DefineKinds.Struct:
                case DefineKinds.Interface:
                    break;

                default:

                    InheritanceSourceTreeItems.Clear();
                    return;
            }

            InheritanceSourceTreeItems.Clear();
            AddInheritanceSourceMember(item, true, string.Empty);
        }

        private void AddInheritanceSourceMember(UserDefinition item, bool isTargetDefinition, string relationName)
        {
            // UserDefinition -> TreeViewItemModel -> DefinitionItemModel
            // メンバー集めは、既存処理を使用する。すでに TreeViewItemModel 用のメソッドがあるので
            // いったんメンバーをくっつけて、それをもらう

            // class, struct, interface
            var containerModel = ConvertTreeViewItemModel(item);
            AddFilteredMember(containerModel, item);

            // 定義元のソースファイルが違う場合、名前空間も付ける
            var definitionName = item.DisplaySignature;
            if (item.SourceFile != SourceFile)
                definitionName = $"{definitionName}（{Path.GetFileName(item.SourceFile)}）";

            // ヘッダー名
            var model = new DefinitionItemModel
            {
                IsTargetDefinition = isTargetDefinition,
                RelationName = relationName,
                DefinitionName = definitionName,
            };

            InheritanceSourceTreeItems.Add(model);

            // メンバー名
            if (containerModel.Children.Any())
            {
                foreach (var child in containerModel.Children)
                    model.MemberTreeItems.Add(child.Copy());

                model.IsExpanded = true;
            }

            // 継承元クラス、インターフェース
            if (item.BaseTypeInfos.Any())
            {
                foreach (var baseType in item.BaseTypeInfos)
                {
                    // VBNet ソースファイルの場合で、定義元ソースコードが見つからなかった場合、
                    // 定義名が C# 形式で表示されるバグの対応
                    if (AppEnv.Languages == Languages.VBNet)
                        baseType.DisplayBaseType = item.ConvertCurrentLanguageType(baseType.DisplayBaseType);

                    AddInheritanceSourceMember(baseType, model.DefinitionName);
                }
            }
        }

        private void AddInheritanceSourceMember(BaseTypeInfo baseType, string relationName)
        {
            // 定義元ソースコードがある場合、メンバーも表示する
            if (baseType.FoundDefinition)
            {
                var baseItem = AppEnv.UserDefinitions.FirstOrDefault(x =>
                {
                    var b1 = (x.SourceFile == baseType.DefinitionSourceFile);
                    var b2 = (x.StartLength == baseType.DefinitionStartLength);
                    var b3 = (x.EndLength == baseType.DefinitionEndLength);
                    if (b1 && b2 && b3)
                        return true;
                    else
                        return false;
                });

                AddInheritanceSourceMember(baseItem, false, relationName);
            }
            else
            {
                // 定義元ソースコードは無いので、名前だけ表示する
                var model = new DefinitionItemModel
                {
                    IsTargetDefinition = false,
                    RelationName = relationName,
                    DefinitionName = baseType.DisplayBaseType,
                };

                InheritanceSourceTreeItems.Add(model);
            }
        }

        // 継承先ツリー

        private void ShowInheritanceDestinationTree()
        {
            var item = FindUserDefinition();
            if (item is null)
            {
                InheritanceDestinationTreeItems.Clear();
                return;
            }

            // 継承できるもの以外は何もしないで抜ける
            switch (item.DefineKinds)
            {
                case DefineKinds.Class:
                case DefineKinds.Struct:
                case DefineKinds.Interface:
                    break;

                default:

                    InheritanceDestinationTreeItems.Clear();
                    return;
            }

            InheritanceDestinationTreeItems.Clear();
            AddInheritanceDestinationMember(item, true, string.Empty);
        }

        private void AddInheritanceDestinationMember(UserDefinition item, bool isTargetDefinition, string relationName)
        {
            // UserDefinition -> TreeViewItemModel -> DefinitionItemModel
            // メンバー集めは、既存処理を使用する。すでに TreeViewItemModel 用のメソッドがあるので
            // いったんメンバーをくっつけて、それをもらう

            // class, struct, interface
            var containerModel = ConvertTreeViewItemModel(item);
            AddFilteredMember(containerModel, item);

            // 定義元のソースファイルが違う場合、名前空間も付ける
            var definitionName = item.DisplaySignature;
            if (item.SourceFile != SourceFile)
                definitionName = $"{definitionName}（{Path.GetFileName(item.SourceFile)}）";

            // ヘッダー名
            var model = new DefinitionItemModel
            {
                IsTargetDefinition = isTargetDefinition,
                RelationName = relationName,
                DefinitionName = definitionName,
            };

            InheritanceDestinationTreeItems.Add(model);

            // メンバー名
            if (containerModel.Children.Any())
            {
                foreach (var child in containerModel.Children)
                    model.MemberTreeItems.Add(child.Copy());

                model.IsExpanded = true;
            }

            // 継承先クラス、インターフェースを取得
            var destItems = AppEnv.UserDefinitions.Where(x =>
            {
                if (x.BaseTypeInfos.Any(y =>
                {
                    if (!y.FoundDefinition)
                        return false;

                    var b1 = (y.DefinitionSourceFile == item.SourceFile);
                    var b2 = (y.DefinitionStartLength == item.StartLength);
                    var b3 = (y.DefinitionEndLength == item.EndLength);
                    if (b1 && b2 && b3)
                        return true;
                    else
                        return false;
                }))
                {
                    return true;
                }

                return false;
            });

            if (!(destItems is null) && destItems.Any())
            {
                foreach (var destItem in destItems)
                    AddInheritanceDestinationMember(destItem, false, model.DefinitionName);
            }
        }

        #endregion

        #region エディタ / 右クリック / コンテキストメニュー / 定義へ移動　のクリック


        public async void MoveDefinitionMenuItem_Click()
        {
            var result = await Util.FindSymbolAtPositionAsync(SourceFile, CaretOffset);

            if (string.IsNullOrEmpty(result.SourceFile))
                return;

            // 定義元を発見した
            if (result.SourceFile == SourceFile)
            {
                // 同じソースファイル内
                CaretOffset = result.Offset;
            }
            else
            {
                // 別のソースファイル内
                MainVM.AddSourceFilePane(result.SourceFile, result.Offset);
            }
        }


        #endregion




    }
}
