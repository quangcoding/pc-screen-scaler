using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace ScreenProfileSwitcher
{
    public class MainWindow : Window
    {
        private List<DisplayInfo> _displays = new List<DisplayInfo>();
        private List<ScreenProfile> _profiles = new List<ScreenProfile>();
        private string _editingProfileId;

        private ComboBox _displayCombo;
        private TextBlock _currentStatusText;
        private StackPanel _profilesPanel;
        private TextBlock _formTitleText;
        private TextBox _nameTextBox;
        private ComboBox _resolutionCombo;
        private ComboBox _scaleCombo;
        private ComboBox _refreshCombo;
        private Button _saveButton;
        private Button _cancelEditButton;
        private Border _formCard;
        private TextBlock _footerStatusText;

        private bool _suppressSelectionEvents;
        private readonly ImageSource _appLogo;

        // Japanese Minimalist Palette (Washi / Sumi / Matcha / Enji)
        private static readonly Brush WashiBg = Freeze(new SolidColorBrush(Color.FromRgb(247, 245, 240)));       // #F7F5F0
        private static readonly Brush ShiroCard = Freeze(new SolidColorBrush(Color.FromRgb(252, 251, 248)));     // #FCFBF8
        private static readonly Brush ActiveRowBg = Freeze(new SolidColorBrush(Color.FromRgb(241, 237, 227)));   // #F1EDE3
        private static readonly Brush Hairline = Freeze(new SolidColorBrush(Color.FromRgb(226, 220, 208)));      // #E2DCD0
        private static readonly Brush SumiInk = Freeze(new SolidColorBrush(Color.FromRgb(42, 40, 37)));          // #2A2825
        private static readonly Brush SumiHover = Freeze(new SolidColorBrush(Color.FromRgb(24, 23, 21)));        // #181715
        private static readonly Brush HoverWarm = Freeze(new SolidColorBrush(Color.FromRgb(239, 235, 226)));     // #EFEBE2
        private static readonly Brush TextPrimary = Freeze(new SolidColorBrush(Color.FromRgb(35, 33, 30)));      // #23211E
        private static readonly Brush TextMuted = Freeze(new SolidColorBrush(Color.FromRgb(122, 117, 108)));     // #7A756C
        private static readonly Brush MatchaBg = Freeze(new SolidColorBrush(Color.FromRgb(231, 238, 228)));      // #E7EEE4
        private static readonly Brush MatchaFg = Freeze(new SolidColorBrush(Color.FromRgb(74, 107, 78)));       // #4A6B4E
        private static readonly Brush EnjiMuted = Freeze(new SolidColorBrush(Color.FromRgb(168, 68, 56)));       // #A84438

        public MainWindow()
        {
            Title = "Screen Profile Switcher";
            Width = 580;
            Height = 690;
            MinWidth = 490;
            MinHeight = 540;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            Background = WashiBg;
            FontFamily = new FontFamily("Segoe UI");

            _appLogo = LoadAppLogo();
            if (_appLogo != null)
            {
                Icon = _appLogo;
            }

            BuildLayout();
            LoadInitialData();

            // UX: Enter saves, Esc closes the form
            _nameTextBox.KeyDown += (s, e) =>
            {
                if (e.Key == Key.Enter) { SaveProfileFromForm(); e.Handled = true; }
            };
            PreviewKeyDown += (s, e) =>
            {
                if (e.Key == Key.Escape && _formCard.Visibility == Visibility.Visible)
                {
                    ResetFormToCreateMode();
                    e.Handled = true;
                }
            };

            // UX: keep "Đang dùng" status in sync when returning to the app (unless user is editing)
            Activated += (s, e) =>
            {
                if (_formCard.Visibility != Visibility.Visible)
                {
                    RefreshDisplayAndUi(null);
                }
            };
        }

        private void BuildLayout()
        {
            var root = new Grid { Margin = new Thickness(24, 20, 24, 18) };
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Header
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // Profiles
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Form
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Footer

            // 1. Zen Header (open on Washi canvas + hairline divider)
            var headerPanel = new StackPanel { Margin = new Thickness(0, 0, 0, 16) };

            var topRow = new Grid();
            topRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            topRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var titleRow = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            if (_appLogo != null)
            {
                var logoImg = new Image
                {
                    Source = _appLogo,
                    Width = 26,
                    Height = 26,
                    Margin = new Thickness(0, 0, 11, 0),
                    VerticalAlignment = VerticalAlignment.Center
                };
                RenderOptions.SetBitmapScalingMode(logoImg, BitmapScalingMode.HighQuality);
                titleRow.Children.Add(logoImg);
            }

            var titleBlock = new TextBlock
            {
                Text = "Cấu hình Màn hình",
                FontSize = 18,
                FontWeight = FontWeights.Normal,
                Foreground = TextPrimary,
                VerticalAlignment = VerticalAlignment.Center
            };
            titleRow.Children.Add(titleBlock);
            Grid.SetColumn(titleRow, 0);
            topRow.Children.Add(titleRow);

            var rightHeader = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            _displayCombo = CreateStyledComboBox(185, 30);
            _displayCombo.Margin = new Thickness(0, 0, 8, 0);
            _displayCombo.SelectionChanged += (s, e) => OnSelectedDisplayChanged();
            rightHeader.Children.Add(_displayCombo);

            var refreshBtn = CreateOutlineButton("Làm mới", 30);
            refreshBtn.Click += (s, e) => RefreshDisplayAndUi("Đã cập nhật trạng thái màn hình.");
            refreshBtn.Margin = new Thickness(0, 0, 8, 0);
            rightHeader.Children.Add(refreshBtn);

            var addBtn = CreateOutlineButton("+", 30);
            addBtn.Width = 30;
            addBtn.Padding = new Thickness(0);
            addBtn.FontSize = 18;
            addBtn.ToolTip = "Thêm profile mới";
            addBtn.Click += (s, e) => ToggleAddForm();
            rightHeader.Children.Add(addBtn);

            Grid.SetColumn(rightHeader, 1);
            topRow.Children.Add(rightHeader);
            headerPanel.Children.Add(topRow);

            var statusBar = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 10, 0, 12)
            };
            statusBar.Children.Add(new Ellipse
            {
                Width = 6,
                Height = 6,
                Fill = MatchaFg,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(2, 1, 8, 0)
            });

            _currentStatusText = new TextBlock
            {
                Text = "Đang đọc thông số màn hình...",
                FontSize = 12.5,
                Foreground = TextMuted,
                VerticalAlignment = VerticalAlignment.Center
            };
            statusBar.Children.Add(_currentStatusText);
            headerPanel.Children.Add(statusBar);

            headerPanel.Children.Add(new Border
            {
                Height = 1,
                Background = Hairline
            });

            Grid.SetRow(headerPanel, 0);
            root.Children.Add(headerPanel);

            // 2. Saved Profiles List (MUJI-style ledger container with hairline dividers)
            var listSection = new Grid { Margin = new Thickness(0, 0, 0, 16) };
            listSection.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            listSection.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            var listHeader = new TextBlock
            {
                Text = "PROFILE ĐÃ LƯU",
                FontSize = 11,
                FontWeight = FontWeights.SemiBold,
                Foreground = TextMuted,
                Margin = new Thickness(2, 0, 0, 8)
            };
            Grid.SetRow(listHeader, 0);
            listSection.Children.Add(listHeader);

            var listContainer = new Border
            {
                Background = ShiroCard,
                BorderBrush = Hairline,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(4)
            };

            var scroll = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
            };
            _profilesPanel = new StackPanel();
            scroll.Content = _profilesPanel;
            listContainer.Child = scroll;

            Grid.SetRow(listContainer, 1);
            listSection.Children.Add(listContainer);

            Grid.SetRow(listSection, 1);
            root.Children.Add(listSection);

            // 3. Add / Edit Profile Form Card (Shoji-style flat panel)
            var formCard = new Border
            {
                Background = ShiroCard,
                BorderBrush = Hairline,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(16, 14, 16, 14),
                Margin = new Thickness(0, 0, 0, 10)
            };

            _formCard = formCard;
            formCard.Visibility = Visibility.Collapsed;
            var formStack = new StackPanel();

            var formHeaderRow = new Grid { Margin = new Thickness(0, 0, 0, 12) };
            formHeaderRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            formHeaderRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            _formTitleText = new TextBlock
            {
                Text = "THÊM PROFILE MỚI",
                FontSize = 11,
                FontWeight = FontWeights.SemiBold,
                Foreground = TextMuted,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(_formTitleText, 0);
            formHeaderRow.Children.Add(_formTitleText);

            var fillCurrentBtn = CreateGhostButton("Lấy từ hiện tại", TextMuted, TextPrimary);
            fillCurrentBtn.FontSize = 11.5;
            fillCurrentBtn.Click += (s, e) => FillFormFromCurrentDisplay();
            Grid.SetColumn(fillCurrentBtn, 1);
            formHeaderRow.Children.Add(fillCurrentBtn);

            formStack.Children.Add(formHeaderRow);

            // Row: Name
            var nameLabel = new TextBlock
            {
                Text = "Tên profile (để trống sẽ tự đặt theo cấu hình)",
                FontSize = 11.5,
                Foreground = TextMuted,
                Margin = new Thickness(0, 0, 0, 4)
            };
            formStack.Children.Add(nameLabel);

            _nameTextBox = new TextBox
            {
                Height = 32,
                Padding = new Thickness(9, 4, 9, 4),
                FontSize = 13,
                Foreground = TextPrimary,
                Background = WashiBg,
                BorderBrush = Hairline,
                BorderThickness = new Thickness(1),
                VerticalContentAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 12)
            };
            formStack.Children.Add(_nameTextBox);

            // Row: Resolution, Scale, Refresh Rate
            var fieldsGrid = new Grid { Margin = new Thickness(0, 0, 0, 14) };
            fieldsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(2, GridUnitType.Star) });
            fieldsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(10) });
            fieldsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.2, GridUnitType.Star) });
            fieldsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(10) });
            fieldsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.2, GridUnitType.Star) });

            _resolutionCombo = CreateStyledComboBox(0, 32);
            _resolutionCombo.SelectionChanged += (s, e) => PopulateRefreshRatesForSelectedResolution(0);
            AddLabeledControl(fieldsGrid, 0, "Độ phân giải", _resolutionCombo);

            _scaleCombo = CreateStyledComboBox(0, 32);
            AddLabeledControl(fieldsGrid, 2, "Scale (DPI)", _scaleCombo);

            _refreshCombo = CreateStyledComboBox(0, 32);
            AddLabeledControl(fieldsGrid, 4, "Tần số quét", _refreshCombo);

            formStack.Children.Add(fieldsGrid);

            // Row: Save & Cancel Buttons
            var actionRow = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
            _cancelEditButton = CreateOutlineButton("Hủy", 32);
            _cancelEditButton.Margin = new Thickness(0, 0, 8, 0);
            _cancelEditButton.Click += (s, e) => ResetFormToCreateMode();
            actionRow.Children.Add(_cancelEditButton);

            _saveButton = CreatePrimaryButton("Lưu Profile", 32, 108);
            _saveButton.Click += (s, e) => SaveProfileFromForm();
            actionRow.Children.Add(_saveButton);

            formStack.Children.Add(actionRow);
            formCard.Child = formStack;
            Grid.SetRow(formCard, 2);
            root.Children.Add(formCard);

            // 4. Footer Status
            _footerStatusText = new TextBlock
            {
                Text = "Sẵn sàng · Nhấn 'Set' để áp dụng nhanh cấu hình màn hình.",
                FontSize = 11.5,
                Foreground = TextMuted,
                Margin = new Thickness(2, 0, 2, 0),
                TextTrimming = TextTrimming.CharacterEllipsis
            };
            Grid.SetRow(_footerStatusText, 3);
            root.Children.Add(_footerStatusText);

            Content = root;
        }

        private void LoadInitialData()
        {
            _displays = DisplayManager.GetActiveDisplays();
            string primaryDev = _displays.Count > 0 ? _displays[0].DeviceName : @"\\.\DISPLAY1";
            _profiles = ProfileStore.LoadOrCreateDefaults(primaryDev);

            PopulateDisplayDropdown();
            FillFormFromCurrentDisplay();
            RenderProfilesList();
        }

        private void RefreshDisplayAndUi(string statusMessage)
        {
            string prevSelectedDev = GetSelectedDisplayName();
            _displays = DisplayManager.GetActiveDisplays();
            PopulateDisplayDropdown(prevSelectedDev);
            RenderProfilesList();
            if (!string.IsNullOrEmpty(statusMessage))
            {
                SetFooterStatus(statusMessage, false);
            }
        }

        private void PopulateDisplayDropdown(string preferredDeviceName = null)
        {
            _suppressSelectionEvents = true;
            _displayCombo.Items.Clear();
            int selectedIndex = 0;

            for (int i = 0; i < _displays.Count; i++)
            {
                var d = _displays[i];
                var item = new ComboBoxItem { Content = d.DisplayLabel, Tag = d };
                _displayCombo.Items.Add(item);

                if (!string.IsNullOrEmpty(preferredDeviceName) &&
                    string.Equals(d.DeviceName, preferredDeviceName, StringComparison.OrdinalIgnoreCase))
                {
                    selectedIndex = i;
                }
            }

            if (_displayCombo.Items.Count > 0)
            {
                _displayCombo.SelectedIndex = selectedIndex;
            }
            _suppressSelectionEvents = false;
            UpdateCurrentDisplayStatusAndFormOptions();
        }

        private void OnSelectedDisplayChanged()
        {
            if (_suppressSelectionEvents) return;
            UpdateCurrentDisplayStatusAndFormOptions();
        }

        private DisplayInfo GetSelectedDisplay()
        {
            var item = _displayCombo.SelectedItem as ComboBoxItem;
            return item != null ? item.Tag as DisplayInfo : _displays.FirstOrDefault();
        }

        private string GetSelectedDisplayName()
        {
            var d = GetSelectedDisplay();
            return d != null ? d.DeviceName : @"\\.\DISPLAY1";
        }

        private void UpdateCurrentDisplayStatusAndFormOptions()
        {
            var d = GetSelectedDisplay();
            if (d == null)
            {
                _currentStatusText.Text = "Không phát hiện thấy màn hình.";
                return;
            }

            _currentStatusText.Text = d.CurrentStatusText.Replace("•", "·");
            PopulateResolutionOptions(d, d.CurrentWidth, d.CurrentHeight, 0);
            PopulateScaleOptions(d, d.CurrentScalePercent);
        }

        private void PopulateResolutionOptions(DisplayInfo display, int selectW, int selectH, int selectRefresh)
        {
            _resolutionCombo.Items.Clear();
            int selectedIdx = 0;

            for (int i = 0; i < display.SupportedResolutions.Count; i++)
            {
                var r = display.SupportedResolutions[i];
                _resolutionCombo.Items.Add(new ComboBoxItem { Content = r.DisplayText, Tag = r });
                if (r.Width == selectW && r.Height == selectH)
                {
                    selectedIdx = i;
                }
            }

            if (_resolutionCombo.Items.Count > 0)
            {
                _resolutionCombo.SelectedIndex = selectedIdx;
            }
            PopulateRefreshRatesForSelectedResolution(selectRefresh);
        }

        private void PopulateRefreshRatesForSelectedResolution(int selectRefresh)
        {
            _refreshCombo.Items.Clear();
            _refreshCombo.Items.Add(new ComboBoxItem { Content = "Auto (Cao nhất)", Tag = 0 });

            var resItem = _resolutionCombo.SelectedItem as ComboBoxItem;
            var resOpt = resItem != null ? resItem.Tag as ResolutionOption : null;
            int selectedIdx = 0;

            if (resOpt != null)
            {
                for (int i = 0; i < resOpt.RefreshRates.Count; i++)
                {
                    int hz = resOpt.RefreshRates[i];
                    _refreshCombo.Items.Add(new ComboBoxItem { Content = hz + " Hz", Tag = hz });
                    if (selectRefresh > 0 && hz == selectRefresh)
                    {
                        selectedIdx = i + 1;
                    }
                }
            }

            _refreshCombo.SelectedIndex = selectedIdx;
        }

        private void PopulateScaleOptions(DisplayInfo display, int selectScale)
        {
            _scaleCombo.Items.Clear();
            var scales = display != null && display.SupportedScales.Count > 0
                ? new List<int>(display.SupportedScales)
                : new List<int> { 100, 125, 150, 175, 200 };

            if (!scales.Contains(selectScale) && selectScale >= 100)
            {
                scales.Add(selectScale);
                scales.Sort();
            }

            int rec = display != null ? display.RecommendedScalePercent : 100;
            int selectedIdx = 0;

            for (int i = 0; i < scales.Count; i++)
            {
                int s = scales[i];
                string text = s == rec ? string.Format("{0}% (Khuyên dùng)", s) : string.Format("{0}%", s);
                _scaleCombo.Items.Add(new ComboBoxItem { Content = text, Tag = s });
                if (s == selectScale)
                {
                    selectedIdx = i;
                }
            }

            if (_scaleCombo.Items.Count > 0)
            {
                _scaleCombo.SelectedIndex = selectedIdx;
            }
        }

        private void RenderProfilesList()
        {
            _profilesPanel.Children.Clear();

            if (_profiles.Count == 0)
            {
                var empty = new TextBlock
                {
                    Text = "Chưa có profile nào · Hãy tạo cấu hình mới ở bên dưới.",
                    Foreground = TextMuted,
                    FontSize = 12.5,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 32, 0, 32)
                };
                _profilesPanel.Children.Add(empty);
                return;
            }

            for (int i = 0; i < _profiles.Count; i++)
            {
                var profile = _profiles[i];
                bool isActive = IsProfileCurrentlyActive(profile);
                bool isLast = i == _profiles.Count - 1;

                var itemRow = new Border
                {
                    Background = isActive ? ActiveRowBg : ShiroCard,
                    BorderBrush = Hairline,
                    BorderThickness = isLast ? new Thickness(0) : new Thickness(0, 0, 0, 1),
                    Padding = new Thickness(16, 13, 16, 13)
                };

                var row = new Grid();
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                // Left info
                var infoStack = new StackPanel { VerticalAlignment = VerticalAlignment.Center };

                var titleLine = new StackPanel { Orientation = Orientation.Horizontal };
                titleLine.Children.Add(new TextBlock
                {
                    Text = profile.Name,
                    FontSize = 14,
                    FontWeight = isActive ? FontWeights.SemiBold : FontWeights.Normal,
                    Foreground = TextPrimary,
                    VerticalAlignment = VerticalAlignment.Center
                });

                if (isActive)
                {
                    var badge = new Border
                    {
                        Background = MatchaBg,
                        CornerRadius = new CornerRadius(3),
                        Padding = new Thickness(7, 2, 7, 2),
                        Margin = new Thickness(10, 0, 0, 0),
                        VerticalAlignment = VerticalAlignment.Center,
                        Child = new TextBlock
                        {
                            Text = "Đang dùng",
                            FontSize = 10.5,
                            FontWeight = FontWeights.SemiBold,
                            Foreground = MatchaFg
                        }
                    };
                    titleLine.Children.Add(badge);
                }

                infoStack.Children.Add(titleLine);
                infoStack.Children.Add(new TextBlock
                {
                    Text = profile.SummaryText.Replace("•", "·"),
                    FontSize = 12,
                    Foreground = TextMuted,
                    Margin = new Thickness(0, 4, 0, 0)
                });

                Grid.SetColumn(infoStack, 0);
                row.Children.Add(infoStack);

                // Right actions
                var btnStack = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(12, 0, 0, 0)
                };

                var p = profile; // capture for lambdas

                var optBtn = CreateGhostButton("\u22EF", TextMuted, TextPrimary);
                optBtn.FontFamily = new FontFamily("Segoe UI Symbol");
                optBtn.FontSize = 18;
                optBtn.Width = 32;
                optBtn.Padding = new Thickness(0);
                optBtn.ToolTip = "Tùy chọn";
                optBtn.Margin = new Thickness(0, 0, 8, 0);
                optBtn.Click += (s, e) =>
                {
                    var menu = new ContextMenu
                    {
                        PlacementTarget = optBtn,
                        Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom
                    };
                    var editItem = new MenuItem { Header = "Sửa" };
                    editItem.Click += (s2, e2) => BeginEditProfile(p);
                    var dupItem = new MenuItem { Header = "Nhân bản" };
                    dupItem.Click += (s2, e2) => DuplicateProfile(p);
                    var delItem = new MenuItem { Header = "Xóa", Foreground = EnjiMuted };
                    delItem.Click += (s2, e2) => DeleteProfile(p);
                    menu.Items.Add(editItem);
                    menu.Items.Add(dupItem);
                    menu.Items.Add(new Separator());
                    menu.Items.Add(delItem);
                    menu.IsOpen = true;
                };
                btnStack.Children.Add(optBtn);

                var setBtn = CreatePrimaryButton("Set", 30, 68);
                if (isActive)
                {
                    setBtn.IsEnabled = false;
                    setBtn.Opacity = 0.35;
                    setBtn.Cursor = Cursors.Arrow;
                    setBtn.ToolTip = "Đang áp dụng";
                }
                setBtn.Click += (s, e) => ApplyProfile(p);
                btnStack.Children.Add(setBtn);

                Grid.SetColumn(btnStack, 1);
                row.Children.Add(btnStack);

                itemRow.Child = row;
                _profilesPanel.Children.Add(itemRow);
            }
        }

        private bool IsProfileCurrentlyActive(ScreenProfile p)
        {
            var d = _displays.FirstOrDefault(x =>
                string.Equals(x.DeviceName, p.DeviceName, StringComparison.OrdinalIgnoreCase))
                ?? _displays.FirstOrDefault(x => x.IsPrimary);

            if (d == null) return false;

            bool resMatch = d.CurrentWidth == p.Width && d.CurrentHeight == p.Height;
            bool scaleMatch = d.CurrentScalePercent == p.ScalePercent;
            bool hzMatch = p.RefreshRate <= 0 || d.CurrentRefreshRate == p.RefreshRate;
            return resMatch && scaleMatch && hzMatch;
        }

        private void ApplyProfile(ScreenProfile profile)
        {
            string msg;
            bool ok = DisplayManager.ApplyProfile(profile, out msg);
            RefreshDisplayAndUi(msg);
            if (!ok)
            {
                SetFooterStatus(msg, true);
            }
        }

        private void FillFormFromCurrentDisplay()
        {
            var d = GetSelectedDisplay();
            if (d == null) return;

            PopulateResolutionOptions(d, d.CurrentWidth, d.CurrentHeight, 0);
            PopulateScaleOptions(d, d.CurrentScalePercent);
            if (string.IsNullOrEmpty(_editingProfileId))
            {
                _nameTextBox.Text = string.Empty;
            }
        }

        private void BeginEditProfile(ScreenProfile profile)
        {
            _editingProfileId = profile.Id;
            _formTitleText.Text = "CHỈNH SỬA PROFILE";
            _saveButton.Content = "Cập nhật";
            _formCard.Visibility = Visibility.Visible;

            _suppressSelectionEvents = true;
            for (int i = 0; i < _displayCombo.Items.Count; i++)
            {
                var item = _displayCombo.Items[i] as ComboBoxItem;
                var d = item != null ? item.Tag as DisplayInfo : null;
                if (d != null && string.Equals(d.DeviceName, profile.DeviceName, StringComparison.OrdinalIgnoreCase))
                {
                    _displayCombo.SelectedIndex = i;
                    break;
                }
            }
            _suppressSelectionEvents = false;

            var selectedDisplay = GetSelectedDisplay();
            if (selectedDisplay != null)
            {
                _currentStatusText.Text = selectedDisplay.CurrentStatusText.Replace("•", "·");
                PopulateResolutionOptions(selectedDisplay, profile.Width, profile.Height, profile.RefreshRate);
                PopulateScaleOptions(selectedDisplay, profile.ScalePercent);
            }

            _nameTextBox.Text = profile.Name;
            _nameTextBox.Focus();
            _nameTextBox.SelectAll();
        }

        private void ResetFormToCreateMode()
        {
            _editingProfileId = null;
            _formTitleText.Text = "THÊM PROFILE MỚI";
            _saveButton.Content = "Lưu Profile";
            _formCard.Visibility = Visibility.Collapsed;
            _nameTextBox.Text = string.Empty;
        }

        private void ToggleAddForm()
        {
            if (_formCard.Visibility == Visibility.Visible && string.IsNullOrEmpty(_editingProfileId))
            {
                ResetFormToCreateMode();
                return;
            }

            ResetFormToCreateMode();
            FillFormFromCurrentDisplay();
            _formCard.Visibility = Visibility.Visible;
            _nameTextBox.Focus();
        }

        private void SaveProfileFromForm()
        {
            var resItem = _resolutionCombo.SelectedItem as ComboBoxItem;
            var resOpt = resItem != null ? resItem.Tag as ResolutionOption : null;
            if (resOpt == null)
            {
                SetFooterStatus("Vui lòng chọn độ phân giải hợp lệ.", true);
                return;
            }

            var scaleItem = _scaleCombo.SelectedItem as ComboBoxItem;
            int scalePercent = scaleItem != null && scaleItem.Tag is int ? (int)scaleItem.Tag : 100;

            var hzItem = _refreshCombo.SelectedItem as ComboBoxItem;
            int refreshRate = hzItem != null && hzItem.Tag is int ? (int)hzItem.Tag : 0;

            string deviceName = GetSelectedDisplayName();
            string name = (_nameTextBox.Text ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(name))
            {
                string tag = ResolutionOption.GetFriendlyTag(resOpt.Width, resOpt.Height);
                string resPart = string.IsNullOrEmpty(tag)
                    ? string.Format("{0}×{1}", resOpt.Width, resOpt.Height)
                    : tag;
                name = string.Format("{0} - Scale {1}%", resPart, scalePercent);
            }

            if (!string.IsNullOrEmpty(_editingProfileId))
            {
                var existing = _profiles.FirstOrDefault(p => p.Id == _editingProfileId);
                if (existing != null)
                {
                    existing.Name = name;
                    existing.DeviceName = deviceName;
                    existing.Width = resOpt.Width;
                    existing.Height = resOpt.Height;
                    existing.RefreshRate = refreshRate;
                    existing.ScalePercent = scalePercent;
                }
                ProfileStore.Save(_profiles);
                ResetFormToCreateMode();
                RenderProfilesList();
                SetFooterStatus("Đã cập nhật profile \"" + name + "\".", false);
            }
            else
            {
                var created = new ScreenProfile
                {
                    Id = Guid.NewGuid().ToString("N"),
                    Name = name,
                    DeviceName = deviceName,
                    Width = resOpt.Width,
                    Height = resOpt.Height,
                    RefreshRate = refreshRate,
                    ScalePercent = scalePercent
                };
                _profiles.Add(created);
                ProfileStore.Save(_profiles);
                ResetFormToCreateMode();
                RenderProfilesList();
                SetFooterStatus("Đã lưu profile mới \"" + name + "\".", false);
            }
        }

        private void DuplicateProfile(ScreenProfile src)
        {
            var copy = new ScreenProfile
            {
                Id = Guid.NewGuid().ToString("N"),
                Name = src.Name + " (bản sao)",
                DeviceName = src.DeviceName,
                Width = src.Width,
                Height = src.Height,
                RefreshRate = src.RefreshRate,
                ScalePercent = src.ScalePercent
            };
            int idx = _profiles.FindIndex(x => x.Id == src.Id);
            _profiles.Insert(idx >= 0 ? idx + 1 : _profiles.Count, copy);
            ProfileStore.Save(_profiles);
            RenderProfilesList();
            SetFooterStatus("Đã nhân bản \"" + src.Name + "\".", false);
        }

        private void DeleteProfile(ScreenProfile profile)
        {
            if (profile == null) return;
            var answer = MessageBox.Show(this,
                "Xóa profile \"" + profile.Name + "\"?",
                "Xác nhận xóa",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question,
                MessageBoxResult.No);
            if (answer != MessageBoxResult.Yes) return;
            _profiles.RemoveAll(p => p.Id == profile.Id);
            if (_editingProfileId == profile.Id)
            {
                ResetFormToCreateMode();
            }
            ProfileStore.Save(_profiles);
            RenderProfilesList();
            SetFooterStatus("Đã xóa profile \"" + profile.Name + "\".", false);
        }

        private const string DefaultFooterText = "Sẵn sàng · Nhấn 'Set' để áp dụng nhanh cấu hình màn hình.";
        private System.Windows.Threading.DispatcherTimer _footerTimer;

        private void SetFooterStatus(string text, bool isError)
        {
            _footerStatusText.Text = text;
            _footerStatusText.Foreground = isError ? EnjiMuted : TextMuted;

            if (_footerTimer == null)
            {
                _footerTimer = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
                _footerTimer.Tick += (s, e) =>
                {
                    _footerTimer.Stop();
                    _footerStatusText.Text = DefaultFooterText;
                    _footerStatusText.Foreground = TextMuted;
                };
            }
            _footerTimer.Stop();
            if (!isError) _footerTimer.Start();
        }

        private static ComboBox CreateStyledComboBox(double minWidth, double height)
        {
            var combo = new ComboBox
            {
                Height = height,
                VerticalContentAlignment = VerticalAlignment.Center,
                Background = WashiBg,
                BorderBrush = Hairline,
                Foreground = TextPrimary,
                FontSize = 12.5
            };
            if (minWidth > 0)
            {
                combo.MinWidth = minWidth;
            }
            return combo;
        }

        private static void AddLabeledControl(Grid grid, int column, string label, Control control)
        {
            var panel = new StackPanel();
            panel.Children.Add(new TextBlock
            {
                Text = label,
                FontSize = 11.5,
                Foreground = TextMuted,
                Margin = new Thickness(0, 0, 0, 5)
            });
            panel.Children.Add(control);
            Grid.SetColumn(panel, column);
            grid.Children.Add(panel);
        }

        private static Button CreatePrimaryButton(string text, double height, double minWidth)
        {
            var btn = new Button
            {
                Content = text,
                Height = height,
                MinWidth = minWidth,
                Padding = new Thickness(14, 0, 14, 0),
                FontSize = 12.5,
                FontWeight = FontWeights.SemiBold,
                Foreground = ShiroCard,
                Background = SumiInk,
                BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand
            };

            btn.Template = BuildRoundedButtonTemplate(3, SumiHover, null);
            return btn;
        }

        private static Button CreateOutlineButton(string text, double height)
        {
            var btn = new Button
            {
                Content = text,
                Height = height,
                Padding = new Thickness(12, 0, 12, 0),
                FontSize = 12,
                Foreground = TextPrimary,
                Background = ShiroCard,
                BorderBrush = Hairline,
                BorderThickness = new Thickness(1),
                Cursor = Cursors.Hand
            };

            btn.Template = BuildRoundedButtonTemplate(3, HoverWarm, null);
            return btn;
        }

        private static Button CreateGhostButton(string text, Brush normalFg, Brush hoverFg)
        {
            var btn = new Button
            {
                Content = text,
                Height = 28,
                Padding = new Thickness(8, 0, 8, 0),
                FontSize = 12,
                Foreground = normalFg,
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand
            };

            btn.Template = BuildRoundedButtonTemplate(3, HoverWarm, hoverFg);
            return btn;
        }

        private static ControlTemplate BuildRoundedButtonTemplate(double radius, Brush hoverBg, Brush hoverFg)
        {
            var template = new ControlTemplate(typeof(Button));
            var borderFactory = new FrameworkElementFactory(typeof(Border), "bd");
            borderFactory.SetValue(Border.CornerRadiusProperty, new CornerRadius(radius));
            borderFactory.SetBinding(Border.BackgroundProperty, new Binding("Background") { RelativeSource = RelativeSource.TemplatedParent });
            borderFactory.SetBinding(Border.BorderBrushProperty, new Binding("BorderBrush") { RelativeSource = RelativeSource.TemplatedParent });
            borderFactory.SetBinding(Border.BorderThicknessProperty, new Binding("BorderThickness") { RelativeSource = RelativeSource.TemplatedParent });
            borderFactory.SetBinding(Border.PaddingProperty, new Binding("Padding") { RelativeSource = RelativeSource.TemplatedParent });

            var presenter = new FrameworkElementFactory(typeof(ContentPresenter), "cp");
            presenter.SetValue(HorizontalAlignmentProperty, HorizontalAlignment.Center);
            presenter.SetValue(VerticalAlignmentProperty, VerticalAlignment.Center);
            borderFactory.AppendChild(presenter);

            template.VisualTree = borderFactory;

            var hoverTrigger = new Trigger { Property = UIElement.IsMouseOverProperty, Value = true };
            hoverTrigger.Setters.Add(new Setter(Border.BackgroundProperty, hoverBg, "bd"));
            if (hoverFg != null)
            {
                hoverTrigger.Setters.Add(new Setter(TextElement.ForegroundProperty, hoverFg, "cp"));
            }
            template.Triggers.Add(hoverTrigger);

            return template;
        }

        private static ImageSource LoadAppLogo()
        {
            try
            {
                var asm = Assembly.GetExecutingAssembly();
                using (var stream = asm.GetManifestResourceStream("ScreenProfileSwitcher.logo.png"))
                {
                    if (stream != null)
                    {
                        var bmp = new BitmapImage();
                        bmp.BeginInit();
                        bmp.CacheOption = BitmapCacheOption.OnLoad;
                        bmp.StreamSource = stream;
                        bmp.EndInit();
                        bmp.Freeze();
                        return bmp;
                    }
                }

                string fallbackPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "assets", "logo.png");
                if (File.Exists(fallbackPath))
                {
                    var bmp = new BitmapImage(new Uri(fallbackPath, UriKind.Absolute));
                    bmp.Freeze();
                    return bmp;
                }
            }
            catch
            {
                // Ignore icon load errors so the app always launches cleanly
            }

            return null;
        }

        private static SolidColorBrush Freeze(SolidColorBrush brush)
        {
            brush.Freeze();
            return brush;
        }
    }
}
