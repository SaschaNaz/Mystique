﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Dulcet.Twitter;
using Inscribe.Communication.Posting;
using Inscribe.Communication.UserStreams;
using Inscribe.Configuration;
using Inscribe.Core;
using Inscribe.Model;
using Inscribe.Storage;
using Inscribe.Subsystems;
using Inscribe.Text;
using Inscribe.ViewModels.Common;
using Inscribe.ViewModels.Dialogs;
using Inscribe.ViewModels.PartBlocks.MainBlock;
using Inscribe.ViewModels.PartBlocks.MainBlock.TimelineChild;
using Livet;
using Livet.Commands;
using Livet.Messaging;

namespace Inscribe.ViewModels.PartBlocks.InputBlock
{
    public class InputBlockViewModel : ViewModel
    {
        public MainWindowViewModel Parent { get; private set; }
        public InputBlockViewModel(MainWindowViewModel parent)
        {
            this.Parent = parent;
            this._imageStackingViewViewModel = new ImageStackingViewViewModel();
            this._userSelectorViewModel = new UserSelectorViewModel();
            this._userSelectorViewModel.LinkChanged += () => this.LinkUserChanged(this.UserSelectorViewModel.LinkElements);
            this._inputUserSelectorViewModel = new UserSelectorViewModel();
            this._inputUserSelectorViewModel.LinkChanged += this.inputLinkUserChanged;
            this._intelliSenseTextBoxViewModel = new IntelliSenseTextBoxViewModel();
            this._intelliSenseTextBoxViewModel.TextChanged += (o, e) => invalidateTagBindState();
            this._intelliSenseTextBoxViewModel.ItemsOpening += (o, e) => _intelliSenseTextBoxViewModel_OnItemsOpening();

            // Listen changing tab
            this.Parent.ColumnOwnerViewModel.CurrentTabChanged += new Action<TabViewModel>(CurrentTabChanged);
            RegisterKeyAssign();
        }

        void CurrentTabChanged(TabViewModel tab)
        {
            this.SetCurrentTab(tab);
        }

        UserSelectorViewModel _userSelectorViewModel;
        public UserSelectorViewModel UserSelectorViewModel
        {
            get { return _userSelectorViewModel; }
        }

        UserSelectorViewModel _inputUserSelectorViewModel;
        public UserSelectorViewModel InputUserSelectorViewModel
        {
            get { return _inputUserSelectorViewModel; }
        }

        private void inputLinkUserChanged()
        {
            if (Setting.Instance.InputExperienceProperty.IsEnabledTemporarilyUserSelection)
            {
                this.OverrideTarget(this.InputUserSelectorViewModel.LinkElements);
            }
            else
            {
                this.LinkUserChanged(this.InputUserSelectorViewModel.LinkElements);
            }
        }

        ImageStackingViewViewModel _imageStackingViewViewModel;
        public ImageStackingViewViewModel ImageStackingViewViewModel
        {
            get { return _imageStackingViewViewModel; }
        }
      
        #region MenuItems

        #region ShowConfigCommand
        DelegateCommand _ShowConfigCommand;

        public DelegateCommand ShowConfigCommand
        {
            get
            {
                if (_ShowConfigCommand == null)
                    _ShowConfigCommand = new DelegateCommand(ShowConfig);
                return _ShowConfigCommand;
            }
        }

        public void ShowConfig()
        {
            var vm = new SettingViewModel();
            Messenger.Raise(new TransitionMessage(vm, "ShowConfig"));
        }
        #endregion

        #region CreateNewColumnCommand
        DelegateCommand _CreateNewColumnCommand;

        public DelegateCommand CreateNewColumnCommand
        {
            get
            {
                if (_CreateNewColumnCommand == null)
                    _CreateNewColumnCommand = new DelegateCommand(CreateNewColumn);
                return _CreateNewColumnCommand;
            }
        }

        private void CreateNewColumn()
        {
            var col = this.Parent.ColumnOwnerViewModel.CreateColumn();
            col.AddNewTabCommand.Execute();
        }
        #endregion

        #region CreateNewTabCommand
        DelegateCommand _CreateNewTabCommand;

        public DelegateCommand CreateNewTabCommand
        {
            get
            {
                if (_CreateNewTabCommand == null)
                    _CreateNewTabCommand = new DelegateCommand(CreateNewTab);
                return _CreateNewTabCommand;
            }
        }

        private void CreateNewTab()
        {
            this.Parent.ColumnOwnerViewModel.CurrentFocusColumn.AddNewTabCommand.Execute();
        }
        #endregion

        #region CollectTabsCommand
        DelegateCommand _CollectTabsCommand;

        public DelegateCommand CollectTabsCommand
        {
            get
            {
                if (_CollectTabsCommand == null)
                    _CollectTabsCommand = new DelegateCommand(CollectTabs);
                return _CollectTabsCommand;
            }
        }

        private void CollectTabs()
        {
            var tabs = this.Parent.ColumnOwnerViewModel.Columns
                .SelectMany(cvm =>
                    cvm.TabItems.ToArray().Select(t =>
                    {
                        cvm.RemoveTab(t);
                        return t;
                    })).ToArray();
            var nc = this.Parent.ColumnOwnerViewModel.CreateColumn();
            tabs.ForEach(t => nc.AddTab(t));
            this.Parent.ColumnOwnerViewModel.GCColumn();
        }
        #endregion
      
        #region ClearClosedTabStackCommand
        DelegateCommand _ClearClosedTabStackCommand;

        public DelegateCommand ClearClosedTabStackCommand
        {
            get
            {
                if (_ClearClosedTabStackCommand == null)
                    _ClearClosedTabStackCommand = new DelegateCommand(ClearClosedTabStack);
                return _ClearClosedTabStackCommand;
            }
        }

        private void ClearClosedTabStack()
        {
            this.Parent.ColumnOwnerViewModel.ClearClosedTab();
        }
        #endregion

        #region ShowAssignViewerCommand
        DelegateCommand _ShowAssignViewerCommand;

        public DelegateCommand ShowAssignViewerCommand
        {
            get
            {
                if (_ShowAssignViewerCommand == null)
                    _ShowAssignViewerCommand = new DelegateCommand(ShowAssignViewer);
                return _ShowAssignViewerCommand;
            }
        }

        private void ShowAssignViewer()
        {
            Messenger.Raise(new TransitionMessage("ShowAssignViewer"));
        }
        #endregion

        #region ReconnectStreamsCommand
        DelegateCommand _ReconnectStreamsCommand;

        public DelegateCommand ReconnectStreamsCommand
        {
            get
            {
                if (_ReconnectStreamsCommand == null)
                    _ReconnectStreamsCommand = new DelegateCommand(ReconnectStreams);
                return _ReconnectStreamsCommand;
            }
        }

        private void ReconnectStreams()
        {
            Task.Factory.StartNew(() => ConnectionManager.RefreshReceivers());
        }
        #endregion

        #region ClearImageCachesCommand
        DelegateCommand _ClearImageCachesCommand;

        public DelegateCommand ClearImageCachesCommand
        {
            get
            {
                if (_ClearImageCachesCommand == null)
                    _ClearImageCachesCommand = new DelegateCommand(ClearImageCaches);
                return _ClearImageCachesCommand;
            }
        }

        private void ClearImageCaches()
        {
            ImageCacheStorage.ClearAllCache();
        }
        #endregion

        #region ShowAboutCommand
        DelegateCommand _ShowAboutCommand;

        public DelegateCommand ShowAboutCommand
        {
            get
            {
                if (_ShowAboutCommand == null)
                    _ShowAboutCommand = new DelegateCommand(ShowAbout);
                return _ShowAboutCommand;
            }
        }

        private void ShowAbout()
        {
            Messenger.Raise(new TransitionMessage("ShowAbout"));
        }
        #endregion

        #region FeedbackCommand
        DelegateCommand _FeedbackCommand;

        public DelegateCommand FeedbackCommand
        {
            get
            {
                if (_FeedbackCommand == null)
                    _FeedbackCommand = new DelegateCommand(Feedback);
                return _FeedbackCommand;
            }
        }

        private void Feedback()
        {
            this.SetOpenText(true, true);
            this.SetText(" #krile");
            this.SetInputCaretIndex(0);
        }
        #endregion

        public bool IsSilentMode
        {
            get { return Setting.Instance.StateProperty.IsInSilentMode; }
            set
            {
                Setting.Instance.StateProperty.IsInSilentMode = value;
                RaisePropertyChanged(() => IsSilentMode);
            }
        }

        
        #region ExitCommand
        DelegateCommand _ExitCommand;

        public DelegateCommand ExitCommand
        {
            get
            {
                if (_ExitCommand == null)
                    _ExitCommand = new DelegateCommand(Exit);
                return _ExitCommand;
            }
        }

        private void Exit()
        {
            KernelService.AppShutdown();
        }
        #endregion
      

        #endregion

        #region Input control

        private InputDescription _currentInputDescription = null;
        public InputDescription CurrentInputDescription
        {
            get
            {
                if (this._currentInputDescription == null)
                {
                    System.Diagnostics.Debug.WriteLine("input description initialized");
                    this._currentInputDescription = new InputDescription(this._intelliSenseTextBoxViewModel);
                    this._currentInputDescription.PropertyChanged += (o, e) => UpdateCommand.RaiseCanExecuteChanged();
                }
                return this._currentInputDescription;
            }
        }

        private IntelliSenseTextBoxViewModel _intelliSenseTextBoxViewModel;
        public IntelliSenseTextBoxViewModel IntelliSenseTextBoxViewModel
        {
            get { return _intelliSenseTextBoxViewModel; }
        }

        void _intelliSenseTextBoxViewModel_OnItemsOpening()
        {
            this._intelliSenseTextBoxViewModel.Items =
                UserStorage.GetAll()
                .Select(u => new IntelliSenseItemViewModel("@" + u.TwitterUser.ScreenName, u.TwitterUser.ProfileImage))
                .Concat(HashtagStorage.GetHashtags()
                    .Select(s => new IntelliSenseItemViewModel("#" + s, null)))
                .OrderBy(s => s.ItemText).ToArray();
        }

        private void ResetInputDescription()
        {
            System.Diagnostics.Debug.WriteLine("input description resetted");
            this._currentInputDescription = null;
            this.OverrideTarget(null);
            RaisePropertyChanged(() => CurrentInputDescription);
            UpdateAccountImages();
        }

        public void SetCurrentTab(TabViewModel tvm)
        {
            if (tvm != null)
                this.UserSelectorViewModel.LinkElements = tvm.TabProperty.LinkAccountInfos;
            else
                this.UserSelectorViewModel.LinkElements = AccountStorage.Accounts;
            UpdateAccountImages();
        }

        public void SetInReplyTo(TweetViewModel tweet)
        {
            if (tweet == null)
            {
                // clear in reply to
                this.CurrentInputDescription.InReplyToId = 0;
            }
            else
            {
                if (this.CurrentInputDescription.InputText.StartsWith(".@"))
                {
                    // multi reply mode
                    string remain;
                    var screens = SplitTweet(this.CurrentInputDescription.InputText, out remain);
                    if (screens.FirstOrDefault(s => s.Equals(tweet.Status.User.ScreenName, StringComparison.CurrentCultureIgnoreCase)) != null)
                    {
                        // 選択ユーザーのスクリーン名だけ抜く
                        this.CurrentInputDescription.InputText = "." +
                            screens.Where(s => !s.Equals(tweet.Status.User.ScreenName, StringComparison.CurrentCultureIgnoreCase))
                            .Select(s => "@" + s)
                            .JoinString(" ") + " " +
                            remain;
                    }
                    else
                    {
                        this.CurrentInputDescription.InputText = "." +
                            screens.Select(s => "@" + s).JoinString(" ") + " " +
                            "@" + tweet.Status.User.ScreenName + " " +
                            remain;
                        this.SetInputCaretIndex(this.CurrentInputDescription.InputText.Length);
                    }
                    this.CurrentInputDescription.InReplyToId = 0;
                }
                else if (this.CurrentInputDescription.InReplyToId != 0 && this.CurrentInputDescription.InputText.StartsWith("@"))
                {
                    // single reply mode -> muliti reply mode
                    if (this.CurrentInputDescription.InReplyToId == tweet.Status.Id)
                    {
                        this.CurrentInputDescription.InputText = "." + this.CurrentInputDescription.InputText;
                        this.CurrentInputDescription.InReplyToId = 0;
                    }
                    else
                    {
                        string remain;
                        var screens = SplitTweet(this.CurrentInputDescription.InputText, out remain);
                        this.CurrentInputDescription.InputText = "." +
                            screens.Select(s => "@" + s).JoinString(" ") + " " +
                            "@" + tweet.Status.User.ScreenName +  " " +
                            remain;
                        this.CurrentInputDescription.InReplyToId = 0;
                        this.SetInputCaretIndex(this.CurrentInputDescription.InputText.Length);
                    }
                    this.overrideTargets = null;
                }
                else
                {
                    // single reply mode
                    this.CurrentInputDescription.InReplyToId = tweet.Status.Id;
                    if (tweet.Status is TwitterDirectMessage)
                    {
                        this.OverrideTarget(new[] { AccountStorage.Get(((TwitterDirectMessage)tweet.Status).Recipient.ScreenName) });
                        this.CurrentInputDescription.InputText = "d @" + tweet.Status.User.ScreenName + " ";
                        this.SetInputCaretIndex(this.CurrentInputDescription.InputText.Length);
                    }
                    else
                    {
                        var mentions = RegularExpressions.AtRegex.Matches(tweet.TweetText);
                        var sns = new[] { "@" + tweet.Status.User.ScreenName }.Concat(mentions.Cast<Match>().Select(m => m.Value))
                            .Distinct().Where(s => !AccountStorage.Contains(s)).ToArray();
                        /*
                        if (tweet.Status is TwitterStatus && AccountStorage.Contains(((TwitterStatus)tweet.Status).InReplyToUserScreenName))
                            sns = sns.Except(new[] { "@" + ((TwitterStatus)tweet.Status).InReplyToUserScreenName }).ToArray();
                        */
                        if (sns.Length > 1)
                        {
                            this.CurrentInputDescription.InputText = sns.JoinString(" ") + " ";
                            this.SetInputCaretIndex(sns[0].Length + 1, sns.JoinString(" ").Length - sns[0].Length);
                        }
                        else 
                        {
                            this.CurrentInputDescription.InputText = "@" + tweet.Status.User.ScreenName + " ";
                            this.SetInputCaretIndex(this.CurrentInputDescription.InputText.Length);
                        }
                        if (tweet.Status is TwitterStatus && AccountStorage.Contains(((TwitterStatus)tweet.Status).InReplyToUserScreenName))
                        {
                            this.OverrideTarget(new[] { AccountStorage.Get(((TwitterStatus)tweet.Status).InReplyToUserScreenName) });
                        }
                    }
                }
            }
        }

        private IEnumerable<string> SplitTweet(string input, out string after)
        {
            List<string> screens = new List<string>();
            if (input.StartsWith("."))
                input = input.Substring(1);
            var splitted = input.Split(' ');
            int i = 0;
            for (; i < splitted.Length; i++)
            {
                if (splitted[i].Length == 0)
                    continue;
                if (splitted[i][0] == '@' &&
                    splitted[i].Skip(1).All(
                    c => (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9') || c == '_'))
                {
                    // valid screen name
                    screens.Add(splitted[i].Substring(1));
                }
                else
                {
                    // invalid screen name
                    break;
                }
            }
            if (i >= splitted.Length)
                after = String.Empty;
            else
                after = splitted.Skip(i).JoinString(" ");
            return screens.Distinct().ToArray();
        }

        public void SetText(string text)
        {
            this.CurrentInputDescription.InputText = text;
        }

        public void SetInputCaretIndex(int selectionStart, int selectionLength = 0)
        {
            this._intelliSenseTextBoxViewModel.SetCaret(selectionStart, selectionLength);
        }

        IEnumerable<AccountInfo> overrideTargets = null;

        /// <summary>
        /// 現在の投稿対象をオーバーライドします。
        /// </summary>
        public void OverrideTarget(IEnumerable<AccountInfo> accounts)
        {
            this.overrideTargets = accounts;
            if (accounts == null)
            {
                var ctab = this.Parent.ColumnOwnerViewModel.CurrentTab;
                this.overrideTargets = null;
                if (ctab == null) return;
                this.InputUserSelectorViewModel.LinkElements = ctab.TabProperty.LinkAccountInfos;
                UpdateAccountImages();
                UpdateCommand.RaiseCanExecuteChanged();
            }
            else
            {
                if (Setting.Instance.InputExperienceProperty.IsEnabledTemporarilyUserSelection)
                    this.InputUserSelectorViewModel.LinkElements = accounts;
                UpdateAccountImages();
                UpdateCommand.RaiseCanExecuteChanged();
            }
        }

        private void LinkUserChanged(IEnumerable<AccountInfo> info)
        {
            var ctab = this.Parent.ColumnOwnerViewModel.CurrentTab;
            this.overrideTargets = null;
            if (ctab == null) return;
            ctab.TabProperty.LinkAccountInfos = info.ToArray();
            this.UserSelectorViewModel.LinkElements = info;
            this.InputUserSelectorViewModel.LinkElements = info;
            UpdateAccountImages();
            UpdateCommand.RaiseCanExecuteChanged();
        }

        private void UpdateAccountImages()
        {
            IEnumerable<AccountInfo> currentTarget = null;
            if (this.overrideTargets != null)
                currentTarget = this.overrideTargets;
            else
                currentTarget = this.UserSelectorViewModel.LinkElements;
            if (currentTarget != null)
                currentTarget = AccountStorage.Accounts.Where(i => currentTarget.Contains(i)).ToArray();
            Task.Factory.StartNew(() => currentTarget.Select(ai => ai.ProfileImage).ToArray())
                .ContinueWith(r => DispatcherHelper.BeginInvoke(() => ImageStackingViewViewModel.ImageUrls = r.Result));
        }

        #endregion

        #region Hashtag Binding

        private ObservableCollection<string> automaticBoundTags = new ObservableCollection<string>();
        public ObservableCollection<string> AutomaticBoundTags
        {
            get { return automaticBoundTags; }
        }

        private ObservableCollection<string> bindingTags = new ObservableCollection<string>();
        public ObservableCollection<string> BindingTags
        {
            get { return bindingTags; }
        }

        private ObservableCollection<string> bindCandidateTags = new ObservableCollection<string>();
        public ObservableCollection<string> BindCandidateTags
        {
            get { return bindCandidateTags; }
        }

        private bool _isEnabledAutoBind = true;
        public bool IsEnabledAutoBind
        {
            get { return this._isEnabledAutoBind; }
            set
            {
                this._isEnabledAutoBind = value;
                RaisePropertyChanged(() => IsEnabledAutoBind);
                this.invalidateTagBindState();
            }
        }

        private void invalidateTagBindState()
        {
            // 自動バインドの処理
            if (this._isEnabledAutoBind)
            {
                var autobinds = Setting.Instance.InputExperienceProperty.HashtagAutoBindDescriptions
                    .Where(d => d.CheckCondition(_intelliSenseTextBoxViewModel.TextBoxText))
                    .Select(d => "#" + d.TagText)
                    .Except(bindingTags).ToArray();
                automaticBoundTags.Except(autobinds).ToArray().ForEach(i => automaticBoundTags.Remove(i));
                autobinds.Except(automaticBoundTags).ForEach(i => automaticBoundTags.Add(i));
            }
            else
            {
                this.automaticBoundTags.Clear();
            }

            // バインドヘルパーの処理
            var unbounds = RegularExpressions.HashRegex.Matches(_intelliSenseTextBoxViewModel.TextBoxText)
                .Cast<Match>().Select(m => m.Value)
                .Except(bindingTags);
            bindCandidateTags.Except(unbounds).ToArray().ForEach(s => bindCandidateTags.Remove(s));
            unbounds.Except(bindCandidateTags).ForEach(s => bindCandidateTags.Add(s));
        }

        #region ClearBindCommand
        DelegateCommand _ClearBindCommand;

        public DelegateCommand ClearBindCommand
        {
            get
            {
                if (_ClearBindCommand == null)
                    _ClearBindCommand = new DelegateCommand(ClearBind);
                return _ClearBindCommand;
            }
        }

        private void ClearBind()
        {
            bindingTags.Clear();
            bindCandidateTags.Clear();
        }
        #endregion

        #region AddBindCommand
        DelegateCommand<string> _AddBindCommand;

        public DelegateCommand<string> AddBindCommand
        {
            get
            {
                if (_AddBindCommand == null)
                    _AddBindCommand = new DelegateCommand<string>(AddBind);
                return _AddBindCommand;
            }
        }

        private void AddBind(string parameter)
        {
            this.bindCandidateTags.Remove(parameter);
            this.bindingTags.Add(parameter);
            invalidateTagBindState();
        }
        #endregion

        #region RemoveBindCommand
        DelegateCommand<string> _RemoveBindCommand;

        public DelegateCommand<string> RemoveBindCommand
        {
            get
            {
                if (_RemoveBindCommand == null)
                    _RemoveBindCommand = new DelegateCommand<string>(RemoveBind);
                return _RemoveBindCommand;
            }
        }

        private void RemoveBind(string parameter)
        {
            this.bindingTags.Remove(parameter);
            // バインド更新
            this.invalidateTagBindState();
        }
        #endregion

        #region EditAutoBindCommand
        DelegateCommand _EditAutoBindCommand;

        public DelegateCommand EditAutoBindCommand
        {
            get
            {
                if (_EditAutoBindCommand == null)
                    _EditAutoBindCommand = new DelegateCommand(EditAutoBind);
                return _EditAutoBindCommand;
            }
        }

        private void EditAutoBind()
        {
            this.Messenger.Raise(new TransitionMessage("EditAutoBind"));
        }
        #endregion
      

        private void ClearCandidates()
        {
            this.BindCandidateTags.Clear();
        }

        #endregion

        #region State control

        private bool _isOpenInput =false;
        public bool IsOpenInput
        {
            get { return this._isOpenInput; }
            private set
            {
                this._isOpenInput = value;
                RaisePropertyChanged(() => IsOpenInput);
            }
        }

        public void SetOpenText(bool isOpen, bool setFocus = false)
        {
            if (this.IsOpenInput != isOpen)
            {
                this.IsOpenInput = isOpen;
                ResetInputDescription();
            }
            if (setFocus && isOpen)
                this._intelliSenseTextBoxViewModel.SetFocus();
        }


        #endregion

        #region Commands

        #region OpenInputCommand
        DelegateCommand _OpenInputCommand;

        public DelegateCommand OpenInputCommand
        {
            get
            {
                if (_OpenInputCommand == null)
                    _OpenInputCommand = new DelegateCommand(OpenInput);
                return _OpenInputCommand;
            }
        }

        private void OpenInput()
        {
            SetOpenText(true, true);
        }
        #endregion

        #region CloseInputCommand
        DelegateCommand _CloseInputCommand;

        public DelegateCommand CloseInputCommand
        {
            get
            {
                if (_CloseInputCommand == null)
                    _CloseInputCommand = new DelegateCommand(CloseInput);
                return _CloseInputCommand;
            }
        }

        private void CloseInput()
        {
            SetOpenText(false);
        }
        #endregion

        #region RemoveInReplyToCommand
        DelegateCommand _RemoveInReplyToCommand;

        public DelegateCommand RemoveInReplyToCommand
        {
            get
            {
                if (_RemoveInReplyToCommand == null)
                    _RemoveInReplyToCommand = new DelegateCommand(RemoveInReplyTo);
                return _RemoveInReplyToCommand;
            }
        }

        private void RemoveInReplyTo()
        {
            this.CurrentInputDescription.InReplyToId = 0;
        }
        #endregion

        #region URL Shortening

        private bool _isUrlShortening = false;
        public bool IsUrlShortening
        {
            get { return this._isUrlShortening; }
            set

            {
                this._isUrlShortening = value;
                RaisePropertyChanged(() => IsUrlShortening);
            }
        }

        #endregion

        #region AttachImageCommand
        DelegateCommand _AttachImageCommand;

        public DelegateCommand AttachImageCommand
        {
            get
            {
                if (_AttachImageCommand == null)
                    _AttachImageCommand = new DelegateCommand(AttachImage);
                return _AttachImageCommand;
            }
        }

        private void AttachImage()
        {
            if (this.CurrentInputDescription.AttachedImage != null)
            {
                this.CurrentInputDescription.AttachedImage = null;
            }
            else
            {
                var ofm = new Livet.Messaging.IO.OpeningFileSelectionMessage("OpenFile");
                ofm.Filter = "画像ファイル|*.jpg; *.png; *.gif; *.bmp|すべてのファイル|*.*";
                ofm.Title = "添付する画像を選択";
                var ret = this.Messenger.GetResponse(ofm);
                if (ret.Response != null)
                {
                    this.CurrentInputDescription.AttachedImage = ret.Response;
                }
            }
        }
        #endregion

        #region UpdateCommand
        DelegateCommand _UpdateCommand;

        public DelegateCommand UpdateCommand
        {
            get
            {
                if (_UpdateCommand == null)
                    _UpdateCommand = new DelegateCommand(Update, CanUpdate);
                return _UpdateCommand;
            }
        }

        private bool CanUpdate()
        {
            return !String.IsNullOrEmpty(this.CurrentInputDescription.InputText)
                && TweetTextCounter.Count(this.CurrentInputDescription.InputText) <= TwitterDefine.TweetMaxLength &&
                (this.overrideTargets != null || this.UserSelectorViewModel.LinkElements.Count() > 0);
        }

        private void Update()
        {
            if (!this.CanUpdate()) return;
            var boundTags = this.automaticBoundTags.Concat(this.bindingTags).Distinct().ToArray();
            var targets = this.overrideTargets;
            if (targets == null)
                targets = this.UserSelectorViewModel.LinkElements;
            var decidedTargets = targets.ToArray();
            if (Setting.Instance.InputExperienceProperty.UseActiveFallback)
                decidedTargets = targets.Select(a => FallbackAccount(a, a)).Distinct().ToArray();
            this.CurrentInputDescription.ReadyUpdate(this, boundTags, decidedTargets).ForEach(AddUpdateWorker);
            ResetInputDescription();
        }

        private AccountInfo FallbackAccount(AccountInfo original, AccountInfo current)
        {
            if (!PostOffice.IsAccountUnderControlled(current))
                return current;
            var fallback = AccountStorage.Get(current.AccoutProperty.FallbackAccount);
            if (fallback == null || fallback == original)
                return current;
            else
                return FallbackAccount(original, fallback);
        }

        #endregion
      
        #endregion

        #region Posing control

        private void AddUpdateWorker(TweetWorker w)
        {
            DispatcherHelper.BeginInvoke(() => this._updateWorkers.Add(w));
            w.RemoveRequired += () => DispatcherHelper.BeginInvoke(() => this._updateWorkers.Remove(w));
            w.WorkerAddRequired += AddUpdateWorker;
            w.DoWork().ContinueWith(t =>
            {
                if (t.Result)
                {
                    Thread.Sleep(Setting.Instance.ExperienceProperty.PostFinishShowLength);
                    DispatcherHelper.BeginInvoke(() => this._updateWorkers.Remove(w));
                }
            });
        }

        ObservableCollection<TweetWorker> _updateWorkers = new ObservableCollection<TweetWorker>();
        public ObservableCollection<TweetWorker> UpdateWorkers
        {
            get { return this._updateWorkers; }
        }

        #endregion
        
        #region KeyAssign

        public void RegisterKeyAssign()
        {
            KeyAssignCore.RegisterOperation("OpenInput", () => this.OpenInput());
            KeyAssignCore.RegisterOperation("CloseInput", () => 
            {
                this.CloseInput();
                this.Parent.ColumnOwnerViewModel.SetFocus();
            });
            KeyAssignCore.RegisterOperation("ToggleAutoBind", () => this.IsEnabledAutoBind = !this.IsEnabledAutoBind);
            KeyAssignCore.RegisterOperation("RemoveInReplyTo", () => this.RemoveInReplyTo());
            KeyAssignCore.RegisterOperation("AttachImage", () => this.AttachImage());
            KeyAssignCore.RegisterOperation("Post", () => this.Update());
            KeyAssignCore.RegisterOperation("PostAndClose", () =>
            {
                if (!this.CanUpdate()) return;
                this.Update();
                this.CloseInput();
                this.Parent.ColumnOwnerViewModel.SetFocus();
            });
            KeyAssignCore.RegisterOperation("ShowConfig", () => this.ShowConfig());
            KeyAssignCore.RegisterOperation("ShowAbout", () => this.ShowAbout());
        }

        #endregion
    }
}
