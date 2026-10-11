using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace HotynHelper
{
    public class MainForm : Form
    {
        // ===== Ключ подставляется из GitHub Secrets при сборке =====
        private const string API_KEY = "OPENROUTER_KEY_PLACEHOLDER";
        private const string API_URL = "https://openrouter.ai/api/v1/chat/completions";
        private const string MODEL   = "deepseek/deepseek-v4-flash:free";

        static readonly Color C_BG       = Color.FromArgb(9, 11, 15);
        static readonly Color C_BG_2     = Color.FromArgb(14, 17, 23);
        static readonly Color C_CARD     = Color.FromArgb(19, 23, 30);
        static readonly Color C_BORDER   = Color.FromArgb(32, 40, 52);
        static readonly Color C_ACCENT   = Color.FromArgb(88, 166, 255);
        static readonly Color C_ACCENT_2 = Color.FromArgb(56, 139, 253);
        static readonly Color C_TEXT     = Color.FromArgb(230, 237, 243);
        static readonly Color C_TEXT_2   = Color.FromArgb(139, 148, 158);
        static readonly Color C_TEXT_3   = Color.FromArgb(88, 96, 105);

        static readonly Font F_H2 = new Font("Segoe UI", 14f, FontStyle.Bold);
        static readonly Font F_TX = new Font("Segoe UI", 10f);
        static readonly Font F_XS = new Font("Segoe UI", 8.5f);

        static readonly HttpClient http = new HttpClient();

        private Panel _topBar = null!;
        private Panel _centerHost = null!;
        private RoundedPanel _card = null!;
        private CustomTextBox _input = null!;
        private RoundButton _btn = null!;
        private Label _lblTitle = null!;
        private Label _lblSub = null!;
        private Panel _resultsHost = null!;
        private RichTextBox _resultsBox = null!;
        private Label _lblStatus = null!;

        public MainForm()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint, true);
            InitializeUI();
            http.Timeout = TimeSpan.FromSeconds(120);
            http.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", $"Bearer {API_KEY}");
            http.DefaultRequestHeaders.TryAddWithoutValidation("HTTP-Referer", "https://github.com/");
            http.DefaultRequestHeaders.TryAddWithoutValidation("X-Title", "Hotyn Helper");
        }

        private void InitializeUI()
        {
            Text = "Hotyn Helper";
            StartPosition = FormStartPosition.CenterScreen;
            MinimumSize = new Size(880, 600);
            Size = new Size(1020, 700);
            BackColor = C_BG;
            ForeColor = C_TEXT;
            Font = F_TX;
            DoubleBuffered = true;

            _topBar = new Panel { Dock = DockStyle.Top, Height = 64, BackColor = C_BG_2 };
            _topBar.Paint += (s, e) =>
            {
                using var p = new Pen(C_BORDER, 1);
                e.Graphics.DrawLine(p, 0, _topBar.Height - 1, _topBar.Width, _topBar.Height - 1);
            };
            Controls.Add(_topBar);

            var logo = new LogoBox { Location = new Point(20, 12), Size = new Size(40, 40) };
            _topBar.Controls.Add(logo);

            var lblLogo = new Label
            {
                Text = "Hotyn",
                Font = new Font("Segoe UI", 13f, FontStyle.Bold),
                ForeColor = C_TEXT, AutoSize = true, BackColor = Color.Transparent
            };
            var lblLogo2 = new Label
            {
                Text = "Helper",
                Font = new Font("Segoe UI", 13f, FontStyle.Regular),
                ForeColor = C_ACCENT, AutoSize = true, BackColor = Color.Transparent
            };
            _topBar.Controls.Add(lblLogo);
            _topBar.Controls.Add(lblLogo2);
            _topBar.Resize += (s, e) =>
            {
                lblLogo.Location = new Point(70, 14);
                lblLogo2.Location = new Point(70 + lblLogo.Width + 4, 14);
            };
            lblLogo.Location = new Point(70, 14);
            lblLogo2.Location = new Point(126, 14);

            string[] chips = { "Правила", "Законы", "Crystal RP", "Карта" };
            int cx = 230;
            for (int i = 0; i < chips.Length; i++)
            {
                var chip = new ChipButton(chips[i], i == 2) { Location = new Point(cx, 17), AutoSize = true };
                string captured = chips[i];
                chip.Click += (s, e) => { _input.TextValue = captured; _input.FocusInput(); };
                _topBar.Controls.Add(chip);
                cx += chip.Width + 8;
            }

            _centerHost = new Panel { Dock = DockStyle.Fill, BackColor = C_BG };
            Controls.Add(_centerHost);
            _centerHost.BringToFront();

            _card = new RoundedPanel
            {
                Size = new Size(660, 340),
                BackColor = C_CARD,
                Radius = 16,
                BorderColor = C_BORDER
            };
            _centerHost.Controls.Add(_card);
            _centerHost.Resize += (s, e) => CenterCard();

            var searchIcon = new SearchIconBox { Size = new Size(72, 72) };
            _card.Controls.Add(searchIcon);

            _lblTitle = new Label
            {
                Text = "Похожих пунктов нет",
                ForeColor = C_TEXT, Font = F_H2,
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent,
                AutoSize = false, Height = 34
            };
            _card.Controls.Add(_lblTitle);

            _lblSub = new Label
            {
                Text = "Опишите ситуацию своими словами: кто, что сделал,\nпри каких обстоятельствах.",
                ForeColor = C_TEXT_2, Font = F_TX,
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent,
                AutoSize = false, Height = 46
            };
            _card.Controls.Add(_lblSub);

            _input = new CustomTextBox
            {
                Placeholder = "Введите вопрос по форуму...",
                Height = 46
            };
            _card.Controls.Add(_input);

            _btn = new RoundButton
            {
                Text = "Разобрать  ▸",
                Height = 40,
                BackColor = C_ACCENT,
                ForeColor = Color.White,
                Radius = 10,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold)
            };
            _btn.Click += async (s, e) => await AskAsync();
            _card.Controls.Add(_btn);

            _input.OnEnterPressed = async () => await AskAsync();

            _resultsHost = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = C_BG,
                Visible = false,
                Padding = new Padding(28, 18, 28, 10)
            };
            _centerHost.Controls.Add(_resultsHost);

            var resultHeader = new Panel { Dock = DockStyle.Top, Height = 40, BackColor = C_BG };
            _resultsHost.Controls.Add(resultHeader);

            var lblRes = new Label
            {
                Text = "Ответ",
                Font = F_H2, ForeColor = C_TEXT, AutoSize = true,
                Location = new Point(0, 4)
            };
            resultHeader.Controls.Add(lblRes);

            var btnBack = new LinkLabel
            {
                Text = "← Новый вопрос",
                Font = F_TX,
                LinkColor = C_ACCENT,
                ActiveLinkColor = C_ACCENT_2,
                VisitedLinkColor = C_ACCENT,
                AutoSize = true,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            btnBack.LinkClicked += (s, e) =>
            {
                _resultsHost.Visible = false;
                _card.Visible = true;
                _card.BringToFront();
                _input.FocusInput();
            };
            resultHeader.Controls.Add(btnBack);
            resultHeader.Resize += (s, e) => { btnBack.Left = resultHeader.Width - btnBack.Width; };

            _resultsBox = new RichTextBox
            {
                Dock = DockStyle.Fill,
                BackColor = C_CARD,
                ForeColor = C_TEXT,
                BorderStyle = BorderStyle.None,
                Font = new Font("Segoe UI", 10f),
                ReadOnly = true,
                ScrollBars = RichTextBoxScrollBars.Vertical
            };
            _resultsHost.Controls.Add(_resultsBox);
            _resultsBox.BringToFront();

            var statusBar = new Panel { Dock = DockStyle.Bottom, Height = 26, BackColor = C_BG_2 };
            statusBar.Paint += (s, e) =>
            {
                using var p = new Pen(C_BORDER, 1);
                e.Graphics.DrawLine(p, 0, 0, statusBar.Width, 0);
            };
            Controls.Add(statusBar);

            _lblStatus = new Label
            {
                Text = "готов",
                ForeColor = C_TEXT_3, Font = F_XS,
                AutoSize = true, Location = new Point(20, 6),
                BackColor = Color.Transparent
            };
            statusBar.Controls.Add(_lblStatus);
        }

        private void CenterCard()
        {
            _card.Left = (_centerHost.ClientSize.Width - _card.Width) / 2;
            _card.Top = Math.Max(60, (_centerHost.ClientSize.Height - _card.Height) / 2 - 20);
            LayoutCard();
        }

        private void LayoutCard()
        {
            int w = _card.Width, h = _card.Height;
            var iconBox = _card.Controls[0];
            iconBox.Left = (w - iconBox.Width) / 2;
            iconBox.Top = 26;

            _lblTitle.Left = 30;
            _lblTitle.Top = 118;
            _lblTitle.Width = w - 60;

            _lblSub.Left = 30;
            _lblSub.Top = 156;
            _lblSub.Width = w - 60;

            int pad = 40, btnW = 160;
            _input.Left = pad;
            _input.Top = h - 100;
            _input.Width = w - pad * 2 - btnW - 12;

            _btn.Left = _input.Right + 12;
            _btn.Top = h - 100;
            _btn.Width = btnW;
        }

        private async Task AskAsync()
        {
            string question = _input.TextValue.Trim();
            if (string.IsNullOrEmpty(question)) return;

            if (API_KEY.Contains("OPENROUTER_KEY_PLACEHOLDER") || API_KEY == "NO_KEY_CONFIGURED")
            {
                _lblTitle.Text = "Ключ не настроен";
                _lblSub.Text = "Добавь OPENROUTER_KEY в GitHub Secrets\nи запусти сборку заново.";
                _lblStatus.Text = "нет ключа";
                return;
            }

            _lblTitle.Text = "Думаю...";
            _lblSub.Text = "DeepSeek обрабатывает твой вопрос";
            _lblStatus.Text = $"вопрос: {question}";
            _btn.Enabled = false;

            try
            {
                string systemPrompt =
                    "Ты — юридический помощник по форуму Crystal Role Play (CrystalRP). " +
                    "Отвечай строго, по делу, со ссылками на правила, как опытный юрист. " +
                    "Структура ответа:\n" +
                    "1. Краткий вывод (1-2 предложения)\n" +
                    "2. Нарушенные правила (пункты, если применимо)\n" +
                    "3. Что делать потерпевшему / ответчику\n" +
                    "4. Возможное наказание\n" +
                    "Пиши по-русски, без воды. Если вопрос не про CrystalRP — скажи об этом.";

                var payload = new
                {
                    model = MODEL,
                    messages = new object[]
                    {
                        new { role = "system", content = systemPrompt },
                        new { role = "user",   content = question   }
                    },
                    temperature = 0.3,
                    stream = false
                };

                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var resp = await http.PostAsync(API_URL, content);
                var respText = await resp.Content.ReadAsStringAsync();

                if (!resp.IsSuccessStatusCode)
                {
                    _lblTitle.Text = "Ошибка " + (int)resp.StatusCode;
                    _lblSub.Text = resp.StatusCode switch
                    {
                        System.Net.HttpStatusCode.Unauthorized => "Ключ неверный. Проверь OPENROUTER_KEY в Secrets.",
                        System.Net.HttpStatusCode.PaymentRequired => "Нужен положительный баланс на openrouter.ai",
                        System.Net.HttpStatusCode.TooManyRequests => "Лимит 50 запросов в день исчерпан. Жди до завтра.",
                        _ => "Сервер ответил ошибкой. Попробуй ещё раз.\n" + respText.Substring(0, Math.Min(200, respText.Length))
                    };
                    _lblStatus.Text = "ошибка " + (int)resp.StatusCode;
                    return;
                }

                using var doc = JsonDocument.Parse(respText);
                var answer = doc.RootElement
                    .GetProperty("choices")[0]
                    .GetProperty("message")
                    .GetProperty("content")
                    .GetString() ?? "Пустой ответ.";

                ShowAnswer(answer);
                _lblStatus.Text = "готово";
            }
            catch (Exception ex)
            {
                _lblTitle.Text = "Ошибка";
                _lblSub.Text = $"Не удалось выполнить запрос:\n{ex.Message}";
                _lblStatus.Text = "ошибка";
            }
            finally
            {
                _btn.Enabled = true;
            }
        }

        private void ShowAnswer(string answer)
        {
            _resultsBox.Clear();
            _resultsBox.SelectionFont = new Font("Segoe UI", 10.5f);
            _resultsBox.SelectionColor = C_TEXT;
            _resultsBox.AppendText(answer);

            _card.Visible = false;
            _resultsHost.Visible = true;
            _resultsHost.BringToFront();
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (_card != null)
            {
                _card.Width = Math.Min(700, _centerHost.ClientSize.Width - 80);
                CenterCard();
            }
        }
    }

    // ==================== КОМПОНЕНТЫ ====================

    public class RoundedPanel : Panel
    {
        public int Radius { get; set; } = 12;
        public Color BorderColor { get; set; } = Color.Transparent;

        public RoundedPanel()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            var r = new Rectangle(0, 0, Width - 1, Height - 1);
            using var path = RoundedRect(r, Radius);
            using var brush = new SolidBrush(BackColor);
            e.Graphics.FillPath(brush, path);
            if (BorderColor != Color.Transparent)
            {
                using var pen = new Pen(BorderColor, 1);
                e.Graphics.DrawPath(pen, path);
            }
        }

        public static GraphicsPath RoundedRect(Rectangle b, int radius)
        {
            var p = new GraphicsPath();
            int d = radius * 2;
            p.AddArc(b.Left, b.Top, d, d, 180, 90);
            p.AddArc(b.Right - d, b.Top, d, d, 270, 90);
            p.AddArc(b.Right - d, b.Bottom - d, d, d, 0, 90);
            p.AddArc(b.Left, b.Bottom - d, d, d, 90, 90);
            p.CloseFigure();
            return p;
        }
    }

    public class RoundButton : RoundedPanel
    {
        public bool Hovered;
        public bool Pressed;

        public RoundButton()
        {
            Radius = 8;
            Cursor = Cursors.Hand;
            MouseEnter += (s, e) => { Hovered = true; Invalidate(); };
            MouseLeave += (s, e) => { Hovered = false; Pressed = false; Invalidate(); };
            MouseDown += (s, e) => { Pressed = true; Invalidate(); };
            MouseUp += (s, e) => { Pressed = false; Invalidate(); };
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            var c = BackColor;
            if (Pressed) c = Darken(c, 0.85f);
            else if (Hovered) c = Lighten(c, 0.10f);

            var r = new Rectangle(0, 0, Width - 1, Height - 1);
            using var path = RoundedRect(r, Radius);
            using var brush = new SolidBrush(c);
            e.Graphics.FillPath(brush, path);

            TextRenderer.DrawText(e.Graphics, Text, Font, r, ForeColor,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        }

        static Color Darken(Color c, float f) => Color.FromArgb(c.A, (int)(c.R * f), (int)(c.G * f), (int)(c.B * f));
        static Color Lighten(Color c, float f) => Color.FromArgb(c.A,
            Math.Min(255, (int)(c.R + (255 - c.R) * f)),
            Math.Min(255, (int)(c.G + (255 - c.G) * f)),
            Math.Min(255, (int)(c.B + (255 - c.B) * f)));
    }

    public class ChipButton : RoundedPanel
    {
        public bool Active { get; set; }
        public bool Hovered;
        private string _text;

        public ChipButton(string text, bool active)
        {
            _text = text;
            Active = active;
            Radius = 8;
            Padding = new Padding(14, 7, 14, 7);
            BackColor = active ? Color.FromArgb(88, 166, 255) : Color.FromArgb(19, 23, 30);
            Cursor = Cursors.Hand;
            AutoSize = true;
            MouseEnter += (s, e) => { Hovered = true; Invalidate(); };
            MouseLeave += (s, e) => { Hovered = false; Invalidate(); };
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            var bg = Active
                ? Color.FromArgb(88, 166, 255)
                : (Hovered ? Color.FromArgb(25, 30, 40) : Color.FromArgb(19, 23, 30));
            var r = new Rectangle(0, 0, Width - 1, Height - 1);
            using var path = RoundedRect(r, Radius);
            using var brush = new SolidBrush(bg);
            e.Graphics.FillPath(brush, path);
            using var pen = new Pen(Active ? Color.FromArgb(88, 166, 255) : Color.FromArgb(32, 40, 52), 1);
            e.Graphics.DrawPath(pen, path);

            var size = TextRenderer.MeasureText(_text, Font);
            Width = size.Width + Padding.Left + Padding.Right;
            Height = size.Height + Padding.Top + Padding.Bottom;

            TextRenderer.DrawText(e.Graphics, _text, Font, r,
                Active ? Color.White : Color.FromArgb(230, 237, 243),
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        }
    }

    public class CustomTextBox : Control
    {
        public string Placeholder { get; set; } = "";
        public string TextValue
        {
            get => _tb.Text;
            set { _tb.Text = value; Invalidate(); }
        }

        private TextBox _tb;
        private bool _focus;
        public Action? OnEnterPressed;

        public CustomTextBox()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
            BackColor = Color.FromArgb(14, 17, 23);

            _tb = new TextBox
            {
                BorderStyle = BorderStyle.None,
                BackColor = Color.FromArgb(14, 17, 23),
                ForeColor = Color.FromArgb(230, 237, 243),
                Font = new Font("Segoe UI", 11f),
                Location = new Point(16, 14),
                Width = 200
            };
            _tb.GotFocus += (s, e) => { _focus = true; Invalidate(); };
            _tb.LostFocus += (s, e) => { _focus = false; Invalidate(); };
            _tb.TextChanged += (s, e) => Invalidate();
            _tb.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; OnEnterPressed?.Invoke(); }
            };
            Controls.Add(_tb);

            Resize += (s, e) => { _tb.Width = Width - 32; _tb.Top = (Height - _tb.Height) / 2; };
            Click += (s, e) => _tb.Focus();
        }

        public void FocusInput() => _tb.Focus();

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            var r = new Rectangle(0, 0, Width - 1, Height - 1);
            using var path = RoundedPanel.RoundedRect(r, 10);
            using var brush = new SolidBrush(BackColor);
            e.Graphics.FillPath(brush, path);
            using var pen = new Pen(_focus ? Color.FromArgb(88, 166, 255) : Color.FromArgb(32, 40, 52), _focus ? 2 : 1);
            e.Graphics.DrawPath(pen, path);

            if (string.IsNullOrEmpty(_tb.Text) && !string.IsNullOrEmpty(Placeholder))
            {
                TextRenderer.DrawText(e.Graphics, Placeholder, new Font("Segoe UI", 11f),
                    new Rectangle(18, 0, Width - 36, Height), Color.FromArgb(88, 96, 105),
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter);
            }
        }
    }

    public class LogoBox : Control
    {
        public LogoBox()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using var pen = new Pen(Color.FromArgb(88, 166, 255), 2);
            int s = Math.Min(Width, Height) - 4;
            var pts = new PointF[6];
            for (int i = 0; i < 6; i++)
            {
                double a = Math.PI / 3 * i - Math.PI / 2;
                pts[i] = new PointF(Width / 2f + (float)(s / 2.0 * Math.Cos(a)),
                                    Height / 2f + (float)(s / 2.0 * Math.Sin(a)));
            }
            e.Graphics.DrawPolygon(pen, pts);
            using var brush = new SolidBrush(Color.FromArgb(88, 166, 255));
            e.Graphics.FillEllipse(brush, Width / 2 - 4, Height / 2 - 4, 8, 8);
        }
    }

    public class SearchIconBox : Control
    {
        public SearchIconBox()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            int w = Width, h = Height;
            using var bgBrush = new SolidBrush(Color.FromArgb(24, 40, 60));
            e.Graphics.FillEllipse(bgBrush, 0, 0, w - 1, h - 1);
            using var pen = new Pen(Color.FromArgb(88, 166, 255), 2);
            e.Graphics.DrawEllipse(pen, 0, 0, w - 1, h - 1);
            using var pen2 = new Pen(Color.FromArgb(88, 166, 255), 2.5f);
            int cx = w / 2, cy = h / 2, r = 14;
            e.Graphics.DrawEllipse(pen2, cx - r, cy - r, r * 2, r * 2);
            e.Graphics.DrawLine(pen2, cx + r - 2, cy + r - 2, cx + r + 8, cy + r + 8);
        }
    }
}
