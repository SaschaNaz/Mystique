﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media;
using Dulcet.Twitter;
using Dulcet.Twitter.Rest;
using Inscribe.Common;
using Inscribe.Communication.Posting;
using Inscribe.Configuration;
using Inscribe.Configuration.Settings;
using Inscribe.Filter;
using Inscribe.Filter.Filters.Numeric;
using Inscribe.Filter.Filters.Particular;
using Inscribe.Storage;
using Inscribe.ViewModels.Dialogs;
using Livet;
using Livet.Commands;
using Livet.Messaging;

namespace Inscribe.ViewModels.PartBlocks.MainBlock.TimelineChild
{
    public class TabDependentTweetViewModel : ViewModel
    {
        public TabViewModel Parent { get; private set; }

        public TweetViewModel Tweet { get; private set; }
        
        public TabDependentTweetViewModel(TweetViewModel tvm, TabViewModel parent)
        {
            if (tvm == null)
                throw new ArgumentNullException("tvm");
            if (parent == null)
                throw new ArgumentNullException("parent");
            this.Parent = parent;
            this.Tweet = tvm;

            switch (Setting.Instance.TimelineExperienceProperty.TimelineItemInitStrategy)
            {
                case ItemInitStrategy.None:
                    break;
                case ItemInitStrategy.DefaultColors:
                    _lightningColorCache = Setting.Instance.ColoringProperty.BaseHighlightColor.GetColor();
                    _foreColorCache = Setting.Instance.ColoringProperty.BaseColor.GetDarkColor();
                    _backColorCache = Setting.Instance.ColoringProperty.BaseColor.GetLightColor();
                    _foreBrushCache = new SolidColorBrush(_foreColorCache).GetAsFrozen() as Brush;
                    _backBrushCache = new SolidColorBrush(_backColorCache).GetAsFrozen() as Brush;
                    break;
                case ItemInitStrategy.Full:
                    CommitColorChanged(true);
                    break;
            }
        }

        #region Binding helper
        private double _tooltipWidth = 0;
        public double TooltipWidth
        {
            get { return _tooltipWidth; }
            set
            {
                _tooltipWidth = value;
                RaisePropertyChanged(() => TooltipWidth);
            }
        }

        private bool _isTextSelected = false;
        public bool IsTextSelected
        {
            get { return _isTextSelected; }
            set
            {
                _isTextSelected = value;
                RaisePropertyChanged(() => IsTextSelected);
            }
        }

        #endregion

        public void SettingValueChanged()
        {
            PendingColorChanged(true);
            this.Tweet.SettingValueChanged();
        }

        /// <summary>
        /// 色の変更があったことを通知します。
        /// </summary>
        /// <param name="isRefreshBrightColor">ブライトカラーを更新するか。ステータスの選択変更に起因する通知の場合はここをfalseに設定します。</param>
        public void PendingColorChanged(bool isRefreshBrightColor = false)
        {
            this.isColorChanged = true;
            if (isRefreshBrightColor)
                this.lightningColorChanged = true;
            // RaisePropertyChanged(() => BackBrush);
            // more speedy
            RaisePropertyChanged("BackBrush");
        }

        #region Coloring Property

        private bool lightningColorChanged = true;
        private bool isColorChanged = true;

        private Color _lightningColorCache;
        public Color LightningColor
        {
            get
            {
                TreatColorChange();
                return _lightningColorCache;
            }
        }

        private Color _foreColorCache;
        public Color ForeColor
        {
            get
            {
                TreatColorChange();
                return _foreColorCache;
            }
        }

        private Color _backColorCache;
        public Color BackColor
        {
            get
            {
                TreatColorChange();
                return _backColorCache;
            }
        }

        private Brush _foreBrushCache;
        public Brush ForeBrush
        {
            get
            {
                TreatColorChange();
                return _foreBrushCache;
            }
        }

        private Brush _backBrushCache;
        public Brush BackBrush
        {
            get
            {
                TreatColorChange();
                return _backBrushCache;
            }
        }

        private Color GetCurrentLightningColor()
        {
            var ptv = this.Parent.TabProperty;
            if (!this.Tweet.Backend.IsDirectMessage)
            {

                if (Setting.Instance.ColoringProperty.MyCurrentTweet.IsActivated &&
                    TwitterHelper.IsMyCurrentTweet(this.Tweet, ptv))
                    return Setting.Instance.ColoringProperty.MyCurrentTweet.GetColor();

                if (Setting.Instance.ColoringProperty.MySubTweet.IsActivated &
                    TwitterHelper.IsMyTweet(this.Tweet))
                    return Setting.Instance.ColoringProperty.MySubTweet.GetColor();

                if (Setting.Instance.ColoringProperty.InReplyToMeCurrent.IsActivated &&
                    TwitterHelper.IsInReplyToMeCurrent(this.Tweet, ptv))
                    return Setting.Instance.ColoringProperty.InReplyToMeCurrent.GetColor();

                if (Setting.Instance.ColoringProperty.InReplyToMeSub.IsActivated &&
                    TwitterHelper.IsInReplyToMe(this.Tweet))
                    return Setting.Instance.ColoringProperty.InReplyToMeSub.GetColor();

                var uvm = UserStorage.Lookup(this.Tweet.Backend.UserId);
                if (uvm != null)
                {

                    if (Setting.Instance.ColoringProperty.Friend.IsActivated &&
                        TwitterHelper.IsFollowingCurrent(uvm, ptv) &&
                        TwitterHelper.IsFollowerCurrent(uvm, ptv))
                        return Setting.Instance.ColoringProperty.Friend.GetColor();

                    if (Setting.Instance.ColoringProperty.Following.IsActivated &&
                        TwitterHelper.IsFollowingCurrent(uvm, ptv))
                        return Setting.Instance.ColoringProperty.Following.GetColor();

                    if (Setting.Instance.ColoringProperty.Follower.IsActivated &&
                        TwitterHelper.IsFollowerCurrent(uvm, ptv))
                        return Setting.Instance.ColoringProperty.Follower.GetColor();

                    if (Setting.Instance.ColoringProperty.Friend.IsActivated &&
                        TwitterHelper.IsFollowingCurrent(uvm, ptv) &&
                        TwitterHelper.IsFollowerCurrent(uvm, ptv))
                        return Setting.Instance.ColoringProperty.Friend.GetColor();

                    if (Setting.Instance.ColoringProperty.Following.IsActivated &&
                        TwitterHelper.IsFollowingCurrent(uvm, ptv))
                        return Setting.Instance.ColoringProperty.Following.GetColor();

                    if (Setting.Instance.ColoringProperty.Follower.IsActivated &&
                        TwitterHelper.IsFollowerCurrent(uvm, ptv))
                        return Setting.Instance.ColoringProperty.Follower.GetColor();

                    if (Setting.Instance.ColoringProperty.FriendAny.IsActivated &&
                        TwitterHelper.IsFollowingAny(uvm) &&
                        TwitterHelper.IsFollowerAny(uvm))
                        return Setting.Instance.ColoringProperty.FriendAny.GetColor();

                    if (Setting.Instance.ColoringProperty.FollowingAny.IsActivated &&
                        TwitterHelper.IsFollowingAny(uvm))
                        return Setting.Instance.ColoringProperty.FollowingAny.GetColor();

                    if (Setting.Instance.ColoringProperty.FollowerAny.IsActivated &&
                        TwitterHelper.IsFollowerAny(uvm))
                        return Setting.Instance.ColoringProperty.FollowerAny.GetColor();
                }
                return Setting.Instance.ColoringProperty.BaseHighlightColor.GetColor();
            }
            else
            {
                if (Setting.Instance.ColoringProperty.DirectMessage.Activated)
                    return Setting.Instance.ColoringProperty.DirectMessage.GetColor(false);
                else
                    return Setting.Instance.ColoringProperty.BaseHighlightColor.GetColor();
            }
        }

        private Color GetCurrentCommonColor(bool dark)
        {
            try
            {
                var pts = Parent.CurrentForegroundTimeline.SelectedTweetViewModel;
                if ((Setting.Instance.ColoringProperty.Selected.IsDarkActivated ||
                    Setting.Instance.ColoringProperty.Selected.IsLightActivated) &&
                    pts != null && pts.Tweet.Backend.UserId == this.Tweet.Backend.UserId &&
                    pts.Tweet.BindingId != this.Tweet.BindingId)
                {
                    if (this.Tweet.Backend.IsDirectMessage)
                    {
                        return RoutePairColor(dark,
                            Setting.Instance.ColoringProperty.Selected,
                            Setting.Instance.ColoringProperty.DirectMessage,
                            Setting.Instance.ColoringProperty.BaseColor);
                    }
                    else if (TwitterHelper.IsPublishedByRetweet(this.Tweet))
                    {
                        return RoutePairColor(dark,
                            Setting.Instance.ColoringProperty.Selected,
                            Setting.Instance.ColoringProperty.Retweeted,
                            Setting.Instance.ColoringProperty.BaseColor);
                    }
                    else
                    {
                        return RoutePairColor(dark,
                            Setting.Instance.ColoringProperty.Selected,
                            Setting.Instance.ColoringProperty.BaseColor);
                    }
                }
                else
                {
                    if (this.Tweet.IsDirectMessage)
                    {
                        return RoutePairColor(dark,
                            Setting.Instance.ColoringProperty.DirectMessage,
                            Setting.Instance.ColoringProperty.BaseColor);
                    }
                    else if (TwitterHelper.IsPublishedByRetweet(this.Tweet))
                    {
                        return RoutePairColor(dark,
                            Setting.Instance.ColoringProperty.Retweeted,
                            Setting.Instance.ColoringProperty.BaseColor);
                    }
                    else
                    {
                        return RoutePairColor(dark,
                            Setting.Instance.ColoringProperty.BaseColor);
                    }
                }
            }
            catch (Exception e)
            {
                ExceptionStorage.Register(e, ExceptionCategory.ConfigurationError,
                    "色設定が破損しています。再試行を押すと、色設定を再作成します。",
                    () => Setting.Instance.ColoringProperty = new ColoringProperty());
                return Colors.Black;
            }
        }

        private Color RoutePairColor(bool dark, params IPairColorElement[] colorProps)
        {
            if (dark)
                return RoutePairColorDark(colorProps);
            else
                return RoutePairColor(colorProps);
        }

        private Color RoutePairColor(params IPairColorElement[] colorProps)
        {
            return colorProps.Where((b) => b.IsLightActivated).Select((b) => b.GetLightColor()).First();
        }

        private Color RoutePairColorDark(params IPairColorElement[] colorProps)
        {
            return colorProps.Where((b) => b.IsDarkActivated).Select((b) => b.GetDarkColor()).First();
        }

        #endregion

        #region Color changing

        static TabDependentTweetViewModel()
        {
            taskDispatcher = new StackTaskDispatcher(1);
            ThreadHelper.Halt += () => taskDispatcher.Dispose();
        }

        private static StackTaskDispatcher taskDispatcher;

        private void TreatColorChange()
        {
            bool change = isColorChanged;
            isColorChanged = false;
            bool lchanged = lightningColorChanged;
            lightningColorChanged = false;
            if (change)
            {
                // 色の更新があった
                taskDispatcher.Push(() => CommitColorChanged(lchanged));
            }
        }

        /// <summary>
        /// このTweetViewModelの色設定を更新します。
        /// </summary>
        private void CommitColorChanged(bool lightningColorUpdated)
        {
            bool nlf = false;
            if (lightningColorUpdated)
            {
                var nlc = GetCurrentLightningColor();
                if (_lightningColorCache != nlc)
                {
                    _lightningColorCache = nlc;
                    nlf = true;
                }
            }

            bool bcf = false;
            var bcc = GetCurrentCommonColor(false);
            if (_backColorCache != bcc)
            {
                _backColorCache = bcc;
                _backBrushCache = new SolidColorBrush(_backColorCache).GetAsFrozen() as Brush;
                bcf = true;
            }

            bool fcf = false;
            var fcc = GetCurrentCommonColor(true);
            if (_foreColorCache != fcc)
            {
                _foreColorCache = fcc;
                _foreBrushCache = new SolidColorBrush(_foreColorCache).GetAsFrozen() as Brush;
                fcf = true;
            }

            if (nlf)
            {
                RaisePropertyChanged(() => LightningColor);
            }
            if (bcf)
            {
                RaisePropertyChanged(() => BackColor);
                RaisePropertyChanged(() => BackBrush);
            }
            if (fcf)
            {
                RaisePropertyChanged(() => ForeColor);
                RaisePropertyChanged(() => ForeBrush);
            }
        }

        #endregion

        public override bool Equals(object obj)
        {
            var tdtv = obj as TabDependentTweetViewModel;
            if (tdtv != null)
                return this.Tweet.Equals(tdtv.Tweet);
            else
                return false;
        }

        public override int GetHashCode()
        {
            return Tweet.GetHashCode();
        }

        #region Navigation Commands

        #region ShowUserDetailCommand
        ViewModelCommand _ShowUserDetailCommand;

        public ViewModelCommand ShowUserDetailCommand
        {
            get
            {
                if (_ShowUserDetailCommand == null)
                    _ShowUserDetailCommand = new ViewModelCommand(ShowUserDetail);
                return _ShowUserDetailCommand;
            }
        }

        private void ShowUserDetail()
        {
            this.Parent.AddTopUser(TwitterHelper.GetSuggestedUser(this.Tweet).Backend.ScreenName);
        }
        #endregion

        #region RetweetedUserDetailCommand
        ViewModelCommand _RetweetedUserDetailCommand;

        public ViewModelCommand RetweetedUserDetailCommand
        {
            get
            {
                if (_RetweetedUserDetailCommand == null)
                    _RetweetedUserDetailCommand = new ViewModelCommand(RetweetedUserDetail);
                return _RetweetedUserDetailCommand;
            }
        }

        private void RetweetedUserDetail()
        {
            this.Parent.AddTopUser(this.Tweet.ScreenName);
        }
        #endregion

        #region DirectMessageReceipientDetailCommand
        ViewModelCommand _DirectMessageReceipientDetailCommand;

        public ViewModelCommand DirectMessageReceipientDetailCommand
        {
            get
            {
                if (_DirectMessageReceipientDetailCommand == null)
                    _DirectMessageReceipientDetailCommand = new ViewModelCommand(DirectMessageReceipientDetail);
                return _DirectMessageReceipientDetailCommand;
            }
        }

        private void DirectMessageReceipientDetail()
        {
            var user = UserStorage.Lookup(this.Tweet.Backend.DirectMessageReceipientId);
            if (user == null) return;
            this.Parent.AddTopUser(user.Backend.ScreenName);
        }
        #endregion

        #region OpenConversationCommand
        ViewModelCommand _OpenConversationCommand;

        public ViewModelCommand OpenConversationCommand
        {
            get
            {
                if (_OpenConversationCommand == null)
                    _OpenConversationCommand = new ViewModelCommand(OpenConversation);
                return _OpenConversationCommand;
            }
        }

        private void OpenConversation()
        {
            var be = this.Tweet.Backend;
            if (be.InReplyToStatusId == 0) return;
            IEnumerable<IFilter> filter = null;
            string description = String.Empty;
            if (Setting.Instance.TimelineExperienceProperty.IsShowConversationAsTree)
            {
                filter = new[] { new FilterMentionTree(be.Id) };
                description = "@#" + be.Id.ToString();
            }
            else
            {
                filter = new[] { new FilterConversation(this.Tweet.ScreenName, be.InReplyToUserScreenName) };
                description = "Cv:@" + this.Tweet.ScreenName + "&@" + be.InReplyToUserScreenName;
            }
            switch (Setting.Instance.TimelineExperienceProperty.ConversationTransition)
            {
                case TransitionMethod.ViewStack:
                    this.Parent.AddTopTimeline(filter);
                    break;
                case TransitionMethod.AddTab:
                    this.Parent.Parent.AddTab(new Configuration.Tabs.TabProperty()
                    {
                        Name = description,
                        TweetSources = filter
                    });
                    break;
                case TransitionMethod.AddColumn:
                    var column = this.Parent.Parent.Parent.CreateColumn();
                    column.AddTab(new Configuration.Tabs.TabProperty()
                    {
                        Name = description,
                        TweetSources = filter
                    });
                    break;
            }
        }
        #endregion

        #region OpenDMConversationCommand
        ViewModelCommand _OpenDMConversationCommand;

        public ViewModelCommand OpenDMConversationCommand
        {
            get
            {
                if (_OpenDMConversationCommand == null)
                    _OpenDMConversationCommand = new ViewModelCommand(OpenDMConversation);
                return _OpenDMConversationCommand;
            }
        }

        private void OpenDMConversation()
        {
            var be = this.Tweet.Backend;
            var recp = UserStorage.Lookup(be.DirectMessageReceipientId);
            if (recp == null) return;
            var filter = new[] { new FilterConversation(this.Tweet.ScreenName, recp.Backend.ScreenName) };
            var description = "DM:@" + this.Tweet.ScreenName + "&@" + recp.Backend.ScreenName;
            switch (Setting.Instance.TimelineExperienceProperty.ConversationTransition)
            {
                case TransitionMethod.ViewStack:
                    this.Parent.AddTopTimeline(filter);
                    break;
                case TransitionMethod.AddTab:
                    this.Parent.Parent.AddTab(new Configuration.Tabs.TabProperty()
                    {
                        Name = description,
                        TweetSources = filter
                    });
                    break;
                case TransitionMethod.AddColumn:
                    var column = this.Parent.Parent.Parent.CreateColumn();
                    column.AddTab(new Configuration.Tabs.TabProperty()
                    {
                        Name = description,
                        TweetSources = filter
                    });
                    break;
            }
        }
        #endregion
      
        #endregion

        #region Timeline Action Commands

        #region MentionCommand
        ViewModelCommand _MentionCommand;

        public ViewModelCommand MentionCommand
        {
            get
            {
                if (_MentionCommand == null)
                    _MentionCommand = new ViewModelCommand(Mention);
                return _MentionCommand;
            }
        }

        private void Mention()
        {
            this.Parent.Parent.Parent.Parent.InputBlockViewModel.SetOpenText(true, true);
            this.Parent.Parent.Parent.Parent.InputBlockViewModel.SetInReplyTo(this.Tweet);
        }
        #endregion

        #region FavoriteCommand
        ViewModelCommand _FavoriteCommand;

        public ViewModelCommand FavoriteCommand
        {
            get
            {
                if (_FavoriteCommand == null)
                    _FavoriteCommand = new ViewModelCommand(FavoriteInternal);
                return _FavoriteCommand;
            }
        }

        private void FavoriteInternal()
        {
            if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
            {
                FavoriteMultiUser();
            }
            else
            {
                ToggleFavorite();

            }
        }
        #endregion

        public void ToggleFavorite()
        {
            if (this.Tweet.Backend.IsDirectMessage) return;
            if (this.Parent.TabProperty.LinkAccountInfos.Select(ai => ai.UserViewModel)
                .All(u => this.Tweet.FavoredUsers.Contains(u)))
            {
                // all account favored
                Unfavorite();
            }
            else
            {
                Favorite();
            }
        }

        public void Favorite()
        {
            if (this.Tweet.Backend.IsDirectMessage) return;
            PostOffice.FavTweet(this.Parent.TabProperty.LinkAccountInfos, this.Tweet);
        }

        public void Unfavorite()
        {
            if (this.Tweet.Backend.IsDirectMessage) return;
            PostOffice.UnfavTweet(this.Parent.TabProperty.LinkAccountInfos, this.Tweet);
        }

        #region FavoriteMultiUserCommand
        ViewModelCommand _FavoriteMultiUserCommand;

        public ViewModelCommand FavoriteMultiUserCommand
        {
            get
            {
                if (_FavoriteMultiUserCommand == null)
                    _FavoriteMultiUserCommand = new ViewModelCommand(FavoriteMultiUser);
                return _FavoriteMultiUserCommand;
            }
        }

        private void FavoriteMultiUser()
        {
            var favored = AccountStorage.Accounts.Where(a => this.Tweet.FavoredUsers.Contains(a.UserViewModel)).ToArray();
            this.Parent.Parent.Parent.Parent.SelectUser(ModalParts.SelectionKind.Favorite,
                favored,
                u =>
                {
                    PostOffice.FavTweet(u.Except(favored), this.Tweet);
                    PostOffice.UnfavTweet(favored.Except(u), this.Tweet);
                });
        }
        #endregion

        #region RetweetCommand
        ViewModelCommand _RetweetCommand;

        public ViewModelCommand RetweetCommand
        {
            get
            {
                if (_RetweetCommand == null)
                    _RetweetCommand = new ViewModelCommand(RetweetInternal);
                return _RetweetCommand;
            }
        }

        private void RetweetInternal()
        {
            if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
            {
                RetweetMultiUser();
            }
            else
            {
                ToggleRetweet();
            }
        }
        #endregion

        public void ToggleRetweet()
        {
            if (this.Tweet.Backend.IsDirectMessage) return;
            if (this.Parent.TabProperty.LinkAccountInfos.Select(ai => ai.UserViewModel)
                .All(u => this.Tweet.RetweetedUsers.Contains(u)))
            {
                // all account favored
                Unretweet();
            }
            else
            {
                Retweet();
            }
        }

        public void Retweet()
        {
            if (this.Tweet.Backend.IsDirectMessage) return;
            PostOffice.Retweet(this.Parent.TabProperty.LinkAccountInfos, this.Tweet);
        }

        public void Unretweet()
        {
            if (this.Tweet.Backend.IsDirectMessage) return;
            PostOffice.Unretweet(this.Parent.TabProperty.LinkAccountInfos, this.Tweet);
        }

        #region RetweetMultiUserCommand
        ViewModelCommand _RetweetMultiUserCommand;

        public ViewModelCommand RetweetMultiUserCommand
        {
            get
            {
                if (_RetweetMultiUserCommand == null)
                    _RetweetMultiUserCommand = new ViewModelCommand(RetweetMultiUser);
                return _RetweetMultiUserCommand;
            }
        }

        private void RetweetMultiUser()
        {
            var retweeted = AccountStorage.Accounts.Where(a => this.Tweet.RetweetedUsers.Contains(a.UserViewModel)).ToArray();
            this.Parent.Parent.Parent.Parent.SelectUser(ModalParts.SelectionKind.Retweet,
                retweeted,
                u =>
                {
                    PostOffice.Retweet(u.Except(retweeted), this.Tweet);
                    PostOffice.Unretweet(retweeted.Except(u), this.Tweet);
                });
        }
        #endregion

        #region UnofficialRetweetCommand
        ViewModelCommand _UnofficialRetweetCommand;

        public ViewModelCommand UnofficialRetweetCommand
        {
            get
            {
                if (_UnofficialRetweetCommand == null)
                    _UnofficialRetweetCommand = new ViewModelCommand(UnofficialRetweet);
                return _UnofficialRetweetCommand;
            }
        }

        private void UnofficialRetweet()
        {
            if (this.Tweet.Backend.IsDirectMessage) return;
            this.Parent.Parent.Parent.Parent.InputBlockViewModel.SetOpenText(true, true);
            var tvm = this.Tweet;
            if (tvm.Backend.RetweetedOriginalId != 0)
                tvm = TweetStorage.Get(tvm.Backend.RetweetedOriginalId);
            this.Parent.Parent.Parent.Parent.InputBlockViewModel.SetText(" RT @" + tvm.ScreenName + ": " + tvm.Text);
            this.Parent.Parent.Parent.Parent.InputBlockViewModel.SetInputCaretIndex(0);
        }
        #endregion
      
        #region QuoteCommand
        ViewModelCommand _QuoteCommand;

        public ViewModelCommand QuoteCommand
        {
            get
            {
                if (_QuoteCommand == null)
                    _QuoteCommand = new ViewModelCommand(Quote);
                return _QuoteCommand;
            }
        }

        private void Quote()
        {
            if (this.Tweet.Backend.IsDirectMessage) return;
            this.Parent.Parent.Parent.Parent.InputBlockViewModel.SetOpenText(true, true);
            this.Parent.Parent.Parent.Parent.InputBlockViewModel.SetInReplyTo(null);
            this.Parent.Parent.Parent.Parent.InputBlockViewModel.SetInReplyTo(this.Tweet);
            var tvm = this.Tweet;
            if (tvm.Backend.RetweetedOriginalId != 0)
                tvm = TweetStorage.Get(tvm.Backend.RetweetedOriginalId);
            this.Parent.Parent.Parent.Parent.InputBlockViewModel.SetText(" QT @" + tvm.ScreenName + ": " + tvm.Text);
            this.Parent.Parent.Parent.Parent.InputBlockViewModel.SetInputCaretIndex(0);

        }
        #endregion

        #region DeleteCommand
        ViewModelCommand _DeleteCommand;

        public ViewModelCommand DeleteCommand
        {
            get
            {
                if (_DeleteCommand == null)
                    _DeleteCommand = new ViewModelCommand(Delete);
                return _DeleteCommand;
            }
        }

        private void Delete()
        {
            if (!this.Tweet.ShowDeleteButton) return;
            var conf = new ConfirmationMessage("ツイート @" + this.Tweet.ScreenName + ": " + this.Tweet.Backend.Text + " を削除してもよろしいですか？", 
                "ツイートの削除", System.Windows.MessageBoxImage.Warning, System.Windows.MessageBoxButton.OKCancel, "Confirm");
            this.Messenger.Raise(conf);
            if (conf.Response.GetValueOrDefault())
            {
                PostOffice.RemoveTweet(AccountStorage.Get(this.Tweet.ScreenName), this.Tweet.BindingId);
            }
        }
        #endregion

        #region ReportForSpamCommand
        ViewModelCommand _ReportForSpamCommand;

        public ViewModelCommand ReportForSpamCommand
        {
            get
            {
                if (_ReportForSpamCommand == null)
                    _ReportForSpamCommand = new ViewModelCommand(ReportForSpam);
                return _ReportForSpamCommand;
            }
        }

        private void ReportForSpam()
        {
            var conf = new ConfirmationMessage("ユーザー @" + this.Tweet.ScreenName + " をスパム報告してもよろしいですか？" + Environment.NewLine +
                "(Krileに存在するすべてのアカウントでスパム報告を行います)",
                "スパム報告の確認", System.Windows.MessageBoxImage.Warning, System.Windows.MessageBoxButton.OKCancel, "Confirm");
            this.Messenger.Raise(conf);
            if (conf.Response.GetValueOrDefault())
            {
                AccountStorage.Accounts.ForEach(i => Task.Factory.StartNew(() => ApiHelper.ExecApi(() => i.ReportSpam(this.Tweet.Backend.UserId))));
                NotifyStorage.Notify("R4Sしました: @" + this.Tweet.ScreenName);
                TweetStorage.Remove(this.Tweet.BindingId);
                Setting.RaiseSettingValueChanged();
            }
        }
        #endregion
      
        #region DeselectCommand
        ViewModelCommand _DeselectCommand;

        public ViewModelCommand DeselectCommand
        {
            get
            {
                if (_DeselectCommand == null)
                    _DeselectCommand = new ViewModelCommand(Deselect);
                return _DeselectCommand;
            }
        }

        private void Deselect()
        {
            this.Parent.CurrentForegroundTimeline.SelectedTweetViewModel = null;
        }
        #endregion

        #region ClickUserIconCommand
        ViewModelCommand _ClickUserIconCommand;

        public ViewModelCommand ClickUserIconCommand
        {
            get
            {
                if (_ClickUserIconCommand == null)
                    _ClickUserIconCommand = new ViewModelCommand(ClickUserIcon);
                return _ClickUserIconCommand;
            }
        }

        private void ClickUserIcon()
        {
            var user = TwitterHelper.GetSuggestedUser(this.Tweet);
            if (Keyboard.Modifiers.HasFlag(ModifierKeys.Alt))
            {
                var cluster = new FilterCluster(){
                    ConcatenateAnd = false,
                    Negate = false,
                    Filters = this.Parent.TabProperty.TweetSources.ToArray()};
                this.Parent.TabProperty.TweetSources = new[]{ cluster.Restrict(new FilterCluster(){ ConcatenateAnd = false, Negate = true, Filters = new[]{ new FilterUserId(user.BindingId) }}).Optimize()}.ToArray();
                Task.Factory.StartNew(() => this.Parent.BaseTimeline.CoreViewModel.InvalidateCache());
            }
            else
            {
                var filter = new[] { new FilterUserId(user.BindingId) };
                var desc = "@" + user.Backend.ScreenName;
                switch (Setting.Instance.TimelineExperienceProperty.UserExtractTransition)
                {
                    case TransitionMethod.ViewStack:
                        this.Parent.AddTopTimeline(filter);
                        break;
                    case TransitionMethod.AddTab:
                        this.Parent.Parent.AddTab(new Configuration.Tabs.TabProperty() { Name = desc, TweetSources = filter });
                        break;
                    case TransitionMethod.AddColumn:
                        var column = this.Parent.Parent.Parent.CreateColumn();
                        column.AddTab(new Configuration.Tabs.TabProperty() { Name = desc, TweetSources = filter });
                        break;
                }
            }
        }
        #endregion

        #region DirectMessageCommand
        ViewModelCommand _DirectMessageCommand;

        public ViewModelCommand DirectMessageCommand
        {
            get
            {
                if (_DirectMessageCommand == null)
                    _DirectMessageCommand = new ViewModelCommand(DirectMessage);
                return _DirectMessageCommand;
            }
        }

        private void DirectMessage()
        {
            this.Parent.Parent.Parent.Parent.InputBlockViewModel
                .SetText("d @" + this.Tweet.ScreenName + " ");
            this.Parent.Parent.Parent.Parent.InputBlockViewModel
                .SetInputCaretIndex(this.Parent.Parent.Parent.Parent.InputBlockViewModel.CurrentInputDescription.InputText.Length);
        }
        #endregion

        #region MuteCommand
        ViewModelCommand _MuteCommand;

        public ViewModelCommand MuteCommand
        {
            get
            {
                if (_MuteCommand == null)
                    _MuteCommand = new ViewModelCommand(Mute);
                return _MuteCommand;
            }
        }

        private void Mute()
        {
            var mvm = new MuteViewModel(this.Tweet);
            this.Messenger.Raise(new TransitionMessage(mvm, "Mute"));
        }
        #endregion

        #region OpenUserCommand
        ListenerCommand<UserViewModel> _OpenUserCommand;

        public ListenerCommand<UserViewModel> OpenUserCommand
        {
            get
            {
                if (_OpenUserCommand == null)
                    _OpenUserCommand = new ListenerCommand<UserViewModel>(OpenUser);
                return _OpenUserCommand;
            }
        }

        private void OpenUser(UserViewModel parameter)
        {
            this.Parent.AddTopUser(parameter.Backend.ScreenName);
        }

        #endregion

        #endregion
    }
}
