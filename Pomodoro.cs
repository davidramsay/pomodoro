// Minimal interval timer with full-screen alerts.
// Build: %WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe /target:winexe /out:Pomodoro.exe Pomodoro.cs
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Media;
using System.Runtime.InteropServices;
using System.Windows.Forms;

class Pomodoro : Form
{
    [DllImport("user32.dll")] static extern bool SetProcessDPIAware();

    readonly NumericUpDown work = new NumericUpDown { Minimum = 1, Maximum = 999, Value = 30 };
    readonly NumericUpDown rest = new NumericUpDown { Minimum = 1, Maximum = 999, Value = 10 };
    readonly Label clock = new Label { AutoSize = true, Font = new Font("Segoe UI", 32), Text = "30:00" };
    readonly Button go = new Button { AutoSize = true, Text = "Start" };
    readonly Timer tick = new Timer { Interval = 250 };
    readonly Timer nag = new Timer { Interval = 8000 };
    readonly List<Overlay> overlays = new List<Overlay>();
    readonly SoundPlayer alarm;
    bool working;
    DateTime end;

    [STAThread]
    static void Main()
    {
        SetProcessDPIAware();
        Application.EnableVisualStyles();
        Application.Run(new Pomodoro());
    }

    static Label Lbl(string s) { return new Label { AutoSize = true, Text = s, Anchor = AnchorStyles.Left }; }

    Pomodoro()
    {
        Text = "Pomodoro";
        Font = new Font("Segoe UI", 10);
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;

        int numWidth = TextRenderer.MeasureText("8888", Font).Width + SystemInformation.VerticalScrollBarWidth + 8;
        work.Width = rest.Width = numWidth;

        var row = new FlowLayoutPanel { AutoSize = true, WrapContents = false };
        row.Controls.AddRange(new Control[] { Lbl("Work"), work, Lbl("Break"), rest, Lbl("min") });
        var root = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.TopDown, Padding = new Padding(10) };
        root.Controls.AddRange(new Control[] { row, clock, go });
        Controls.Add(root);

        string wav = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), @"Media\Alarm01.wav");
        if (File.Exists(wav)) alarm = new SoundPlayer(wav);

        go.Click += (s, e) => { if (tick.Enabled) Stop(); else Begin(true); };
        tick.Tick += (s, e) => OnTick();
        nag.Tick += (s, e) => Nag();
        work.ValueChanged += (s, e) => { if (!tick.Enabled) clock.Text = Fmt(TimeSpan.FromMinutes((double)work.Value)); };
    }

    static string Fmt(TimeSpan t)
    {
        int s = (int)Math.Ceiling(t.TotalSeconds);
        return string.Format("{0:00}:{1:00}", s / 60, s % 60);
    }

    void Begin(bool isWork)
    {
        working = isWork;
        end = DateTime.Now.AddMinutes((double)(isWork ? work.Value : rest.Value));
        go.Text = "Stop";
        tick.Start();
        OnTick();
    }

    void Stop()
    {
        tick.Stop();
        go.Text = "Start";
        Text = "Pomodoro";
        clock.Text = Fmt(TimeSpan.FromMinutes((double)work.Value));
    }

    void OnTick()
    {
        TimeSpan left = end - DateTime.Now;
        if (left <= TimeSpan.Zero) { tick.Stop(); Alert(); return; }
        clock.Text = Fmt(left);
        Text = clock.Text + (working ? " work" : " break");
    }

    void Alert()
    {
        clock.Text = "00:00";
        Text = "TIME'S UP";
        string msg = working ? "Break time.\nLook away from the screen." : "Break's over.";
        string btn = working ? string.Format("Start {0} min break", rest.Value) : string.Format("Start {0} min work", work.Value);
        foreach (Screen s in Screen.AllScreens)
        {
            var o = new Overlay(s, msg, btn);
            o.Dismissed += Dismiss;
            overlays.Add(o);
            o.Show();
        }
        Nag();
        nag.Start();
    }

    // Re-sound the alarm and shove the overlays back on top until dismissed.
    void Nag()
    {
        if (alarm != null) alarm.Play(); else SystemSounds.Exclamation.Play();
        foreach (var o in overlays) { o.TopMost = true; o.BringToFront(); }
        if (overlays.Count > 0) overlays[0].Activate();
    }

    void Dismiss()
    {
        nag.Stop();
        if (alarm != null) alarm.Stop();
        foreach (var o in overlays) o.Release();
        overlays.Clear();
        Begin(!working);
    }
}

class Overlay : Form
{
    public event Action Dismissed;
    readonly Label msg;
    readonly Button ok;
    bool release;

    public Overlay(Screen screen, string text, string button)
    {
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.Manual;
        Bounds = screen.Bounds;
        TopMost = true;
        ShowInTaskbar = false;
        BackColor = Color.FromArgb(24, 24, 24);
        ForeColor = Color.FromArgb(220, 220, 220);

        msg = new Label { AutoSize = true, Text = text, Font = new Font("Segoe UI", 40), TextAlign = ContentAlignment.MiddleCenter };
        // No TabStop so a stray Enter/Space while typing can't dismiss it; it takes a deliberate click.
        ok = new Button { AutoSize = true, Text = button, Font = new Font("Segoe UI", 16), TabStop = false, FlatStyle = FlatStyle.Flat, Padding = new Padding(12, 6, 12, 6) };
        ok.Click += (s, e) => { if (Dismissed != null) Dismissed(); };
        Controls.Add(msg);
        Controls.Add(ok);
    }

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        msg.Location = new Point((ClientSize.Width - msg.Width) / 2, ClientSize.Height / 2 - msg.Height);
        ok.Location = new Point((ClientSize.Width - ok.Width) / 2, ClientSize.Height / 2 + ok.Height);
        ActiveControl = null;
    }

    public void Release() { release = true; Close(); }

    // Block Alt+F4; still let Windows shut down.
    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (!release && e.CloseReason == CloseReason.UserClosing) e.Cancel = true;
        base.OnFormClosing(e);
    }
}
