﻿using System;
using System.Linq;
using System.Threading.Tasks;
using Dulcet.Twitter.Rest;
using Inscribe.Common;
using Inscribe.Configuration;
using Inscribe.Filter;
using Inscribe.Filter.Filters.Numeric;
using Inscribe.Storage;
using Inscribe.ViewModels.Behaviors.Messaging;
using Inscribe.ViewModels.Dialogs.Common;
using Livet;
using Livet.Commands;
using Livet.Messaging;

namespace Inscribe.ViewModels.PartBlocks.MainBlock
{
    public class UserPageViewModel : TimelineCoreViewModelBase
    {
        public TabViewModel Parent { get; private set; }

        public override bool IsActive
        {
            get { return Parent.CurrentForegroundTimeline == this; }
        }

        public override void InvalidateIsActive()
        {
            RaisePropertyChanged(() => IsActive);
        }

        public UserPageViewModel(TabViewModel parent, string userId)
        {
            this.Parent = parent;
            this._timelineListCoreViewModel = new TimelineListCoreViewModel(parent, new IFilter[0]);
            this.SetUser(userId);
        }

        private TimelineListCoreViewModel _timelineListCoreViewModel;
        public TimelineListCoreViewModel TimelineListCoreViewModel
        {
            get { return _timelineListCoreViewModel; }
        }

        public override TimelineListCoreViewModel CoreViewModel
        {
            get { return TimelineListCoreViewModel; }
        }

        private UserViewModel _user = null;
        public UserViewModel User
        {
            get { return _user; }
            set
            {
                if (value == null ?
                    _user == null :
                    _user != null && _user.BackEnd.ScreenName == value.BackEnd.ScreenName)
                    return;
                _user = value;
                RaisePropertyChanged(() => User);
                RaisePropertyChanged(() => UserProfileImageUrl);
                RaisePropertyChanged(() => ScreenName);
                RaisePropertyChanged(() => Name);
                RaisePropertyChanged(() => IsVerified);
                RaisePropertyChanged(() => IsProtected);
                RaisePropertyChanged(() => Location);
                RaisePropertyChanged(() => ProfileUrl);
                RaisePropertyChanged(() => Bio);
                RaisePropertyChanged(() => Tweets);
                RaisePropertyChanged(() => Favorites);
                RaisePropertyChanged(() => Following);
                RaisePropertyChanged(() => Followers);
                RaisePropertyChanged(() => Listed);
                SetUserTimeline(value);
            }
        }

        private bool _isStandby = true;

        public bool IsStandby
        {
            get { return _isStandby; }
            set
            {
                _isStandby = value;
                RaisePropertyChanged(() => IsStandby);
            }
        }

        public Uri UserProfileImageUrl
        {
            get
            {
                return User != null ? User.BackEnd.ProfileImage : null;
            }
        }

        public string ScreenName
        {
            get
            {
                return User != null ? User.BackEnd.ScreenName : String.Empty;
            }
        }

        public string Name
        {
            get
            {
                return User != null ? User.BackEnd.UserName : String.Empty;
            }
        }

        public bool IsVerified
        {
            get
            {
                return User != null && User.BackEnd.IsVerified;
            }
        }

        public bool IsProtected
        {
            get
            {
                return User != null && User.BackEnd.IsProtected;
            }
        }

        public string Location
        {
            get
            {
                return User != null ? User.BackEnd.Location : String.Empty;
            }
        }

        public string ProfileUrl
        {
            get
            {
                return User != null ? User.BackEnd.Web : String.Empty;
            }
        }

        #region OpenLinkCommand
        ListenerCommand<string> _OpenLinkCommand;

        public ListenerCommand<string> OpenLinkCommand
        {
            get
            {
                if (_OpenLinkCommand == null)
                    _OpenLinkCommand = new ListenerCommand<string>(OpenLink);
                return _OpenLinkCommand;
            }
        }

        private void OpenLink(string parameter)
        {
            if (String.IsNullOrEmpty(parameter))
                Browser.Start("http://twitter.com/" + User.BackEnd.ScreenName);
            else
                Browser.Start("http://twitter.com/" + User.BackEnd.ScreenName + "/" + parameter);
        }
        #endregion

        #region OpenUserWebCommand
        ViewModelCommand _OpenUserWebCommand;

        public ViewModelCommand OpenUserWebCommand
        {
            get
            {
                if (_OpenUserWebCommand == null)
                    _OpenUserWebCommand = new ViewModelCommand(OpenUserWeb);
                return _OpenUserWebCommand;
            }
        }

        private void OpenUserWeb()
        {
            if (User != null && User.BackEnd.Web != null)
                Browser.Start(User.BackEnd.Web);
        }
        #endregion

        public string Bio
        {
            get
            {
                return User != null ? User.BackEnd.Bio : String.Empty;
            }
        }

        public string Tweets
        {
            get
            {
                return User != null ? User.BackEnd.Tweets.ToString() : null;
            }
        }

        public string Favorites
        {
            get
            {
                return User != null ? User.BackEnd.Favorites.ToString() : String.Empty;
            }
        }

        private bool _inputMode = false;
        public bool InputMode
        {
            get { return _inputMode; }
            private set
            {
                _inputMode = value;
                RaisePropertyChanged(() => InputMode);
            }
        }

        private string _editScreenName = String.Empty;
        public string EditScreenName
        {
            get { return _editScreenName; }
            set
            {
                _editScreenName = value;
                RaisePropertyChanged(() => EditScreenName);
            }
        }

        public string Following
        {
            get
            {
                return User != null ? User.BackEnd.Followings.ToString() : null;
            }
        }

        public string Followers
        {
            get
            {
                return User != null ? User.BackEnd.Followers.ToString() : null;
            }
        }

        public string Listed
        {
            get
            {
                return User != null ? User.BackEnd.Listed.ToString() : null;
            }
        }

        #region CreateUserTabCommand
        ViewModelCommand _CreateUserTabCommand;

        public ViewModelCommand CreateUserTabCommand
        {
            get
            {
                if (_CreateUserTabCommand == null)
                    _CreateUserTabCommand = new ViewModelCommand(CreateUserTab);
                return _CreateUserTabCommand;
            }
        }

        private void CreateUserTab()
        {
            var desc = "@" + this.User.BackEnd.ScreenName;
            var filt = new[]{ new FilterCluster(){
                 Filters = new[]{new FilterUserId(this.User.BackEnd.Id)}}};

            switch (Setting.Instance.TimelineExperienceProperty.UserOpenTransition)
            {
                case Configuration.Settings.TransitionMethod.ViewStack:
                    this.Parent.AddTopTimeline(filt);
                    break;
                case Configuration.Settings.TransitionMethod.AddTab:
                    this.Parent.Parent.AddTab(
                        new Configuration.Tabs.TabProperty()
                        {
                            Name = desc,
                            TweetSources = filt
                        });
                    break;
                case Configuration.Settings.TransitionMethod.AddColumn:
                    var col = this.Parent.Parent.Parent.CreateColumn();
                    col.AddTab(new Configuration.Tabs.TabProperty()
                    {
                        Name = desc,
                        TweetSources = filt
                    });
                    break;
            }
        }
        #endregion

        #region ReceiveTimelineCommand
        ViewModelCommand _ReceiveTimelineCommand;

        public ViewModelCommand ReceiveTimelineCommand
        {
            get
            {
                if (_ReceiveTimelineCommand == null)
                    _ReceiveTimelineCommand = new ViewModelCommand(ReceiveTimeline);
                return _ReceiveTimelineCommand;
            }
        }

        private void ReceiveTimeline()
        {
            if (User == null) return;
            IsStandby = false;
            Task.Factory.StartNew(() =>
            {
                try
                {
                    var acc = AccountStorage.GetRandom(a => a.Followings.Contains(this.User.BackEnd.Id), true);
                    var tweets = ApiHelper.ExecApi(() => acc.GetUserTimeline(userId: this.User.BackEnd.Id, count: 100, includeRts: true));
                    if (tweets != null)
                        tweets.ForEach(t => TweetStorage.Register(t));
                }
                catch (Exception e)
                {
                    ExceptionStorage.Register(e, ExceptionCategory.TwitterError, "ユーザータイムラインを受信できませんでした: @" + this.User.BackEnd.ScreenName, ReceiveTimeline);
                }
                finally
                {
                    IsStandby = true;
                }
            });
        }
        #endregion

        private void SetUserTimeline(UserViewModel user)
        {
            if (user == null) return;
            this.TimelineListCoreViewModel.Sources = new[] { new FilterUserId(user.BackEnd.Id) };
            Task.Factory.StartNew(() => this.TimelineListCoreViewModel.InvalidateCache());
        }

        public event Action CloseRequired = () => { };

        #region ManageFollowCommand
        ViewModelCommand _ManageFollowCommand;

        public ViewModelCommand ManageFollowCommand
        {
            get
            {
                if (_ManageFollowCommand == null)
                    _ManageFollowCommand = new ViewModelCommand(ManageFollow);
                return _ManageFollowCommand;
            }
        }

        private void ManageFollow()
        {
            this.Messenger.Raise(new TransitionMessage(new FollowManagerViewModel(this.User), "ShowFollowManager"));
        }
        #endregion

        #region EditUserCommand
        ViewModelCommand _EditUserCommand;

        public ViewModelCommand EditUserCommand
        {
            get
            {
                if (_EditUserCommand == null)
                    _EditUserCommand = new ViewModelCommand(EditUser);
                return _EditUserCommand;
            }
        }

        private void EditUser()
        {
            EditScreenName = ScreenName;
            InputMode = true;
            this.Messenger.Raise(new Livet.Messaging.InteractionMessage("FocusToInput"));
        }
        #endregion

        #region EditFinishCommand
        ListenerCommand<String> _EditFinishCommand;

        public ListenerCommand<String> EditFinishCommand
        {
            get
            {
                if (_EditFinishCommand == null)
                    _EditFinishCommand = new ListenerCommand<String>(EditFinish);
                return _EditFinishCommand;
            }
        }

        private void EditFinish(String parameter)
        {
            bool condition;
            if (Boolean.TryParse(parameter, out condition) && condition &&
                !String.IsNullOrEmpty(EditScreenName))
                this.SetUser(EditScreenName);
            else
                InputMode = false;
        }
        #endregion

        /// <summary>
        /// ユーザーを設定します。<para />
        /// NullかString.Emptyが指定されると、ユーザー編集モードに入ります。
        /// </summary>
        internal void SetUser(string screenName)
        {
            if (String.IsNullOrEmpty(screenName))
            {
                User = null;
                InputMode = true;
                this.Messenger.Raise(new Livet.Messaging.InteractionMessage("FocusToInput"));
            }
            else
            {
                InputMode = false;
                screenName = screenName.TrimStart('@', ' ', '\t');
                this.IsStandby = false;
                Task.Factory.StartNew(() =>
                {
                    try
                    {
                        var user = UserStorage.Lookup(screenName);
                        if (user == null)
                        {
                            var cred = AccountStorage.GetRandom();
                            if (cred != null)
                            {
                                var ud = ApiHelper.ExecApi(() => cred.GetUserByScreenName(screenName));
                                if (ud == null)
                                {
                                    DispatcherHelper.BeginInvoke(() => this.Messenger.Raise(new Livet.Messaging.InformationMessage(
                                                "ユーザー @" + screenName + " の情報を取得できません。" + Environment.NewLine +
                                                "ユーザーが存在しない可能性があります。",
                                                "ユーザー情報取得エラー", System.Windows.MessageBoxImage.Warning,
                                                "InformationMessage")));
                                    return;
                                }
                                else
                                {
                                    user = UserStorage.Get(ud);
                                }
                            }
                        }
                        if (user == null)
                            throw new Exception("ユーザー情報がありません。");
                        User = user;
                    }
                    catch(Exception e)
                    {
                        ExceptionStorage.Register(e, ExceptionCategory.TwitterError, "ユーザー @" + screenName + " の情報を取得できませんでした。");
                        DispatcherHelper.BeginInvoke(() => this.Messenger.Raise(new Livet.Messaging.InformationMessage(
                                    "ユーザー @" + screenName + "の情報を取得できません。",
                                    "ユーザー情報取得エラー", System.Windows.MessageBoxImage.Warning,
                                    "InformationMessage")));
                    }
                    finally
                    {
                        IsStandby = true;
                    }
                });
            }
        }

        #region GetFocusCommand
        ViewModelCommand _GetFocusCommand;

        public ViewModelCommand GetFocusCommand
        {
            get
            {
                if (_GetFocusCommand == null)
                    _GetFocusCommand = new ViewModelCommand(GetFocus);
                return _GetFocusCommand;
            }
        }

        private void GetFocus()
        {
            this.OnGetFocus();
        }
        #endregion

        public override void SetSelect(ListSelectionKind kind)
        {
            this.TimelineListCoreViewModel.SetSelect(kind);
        }

        public override TimelineChild.TabDependentTweetViewModel SelectedTweetViewModel
        {
            get
            {
                return this.TimelineListCoreViewModel.SelectedTweetViewModel;
            }
            set
            {
                this.TimelineListCoreViewModel.SelectedTweetViewModel = value;
            }
        }

        public override void InvalidateCache()
        {
            this.TimelineListCoreViewModel.InvalidateCache();
        }
    }
}