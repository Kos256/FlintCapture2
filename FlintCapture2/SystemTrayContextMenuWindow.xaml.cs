using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace FlintCapture2
{
    public partial class SystemTrayContextMenuWindow : Window
    {
        private readonly MainWindow mainWin;

        public SystemTrayContextMenuWindow(MainWindow mainWin)
        {
            InitializeComponent();
            this.mainWin = mainWin;
        }

        // =========================
        // Show menu logic
        // =========================
        public void ShowMenu()
        {
            Show();
            Activate();

            mainWin.GMouseHook.LeftMouseDown += GMouseHook_MouseDown;
            mainWin.GMouseHook.MiddleMouseDown += GMouseHook_MouseDown;
            mainWin.GMouseHook.RightMouseDown += GMouseHook_MouseDown;
        }

        private void GMouseHook_MouseDown(object? sender, Scripts.MouseHookEventArgs e)
        {
            // TODO: fill in
        }

        // =========================
        // Menu Actions
        // =========================

        private void OpenApp_MouseDown(object sender, MouseButtonEventArgs e)
        {
            // TODO: fill in
        }

        private void ScreenshotHistory_MouseDown(object sender, MouseButtonEventArgs e)
        {
            // TODO: fill in
        }

        private void Exit_MouseDown(object sender, MouseButtonEventArgs e)
        {
            // TODO: fill in
        }

        private void ForceExit_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // TODO: fill in
        }

        private void ForceExit_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            // TODO: fill in
        }

        // =========================
        // Hover Animations
        // =========================

        private void OpenApp_MouseEnter(object sender, MouseEventArgs e)
        {
            // TODO: fill in
        }

        private void OpenApp_MouseLeave(object sender, MouseEventArgs e)
        {
            // TODO: fill in
        }

        private void ScreenshotHistory_MouseEnter(object sender, MouseEventArgs e)
        {
            // TODO: fill in
        }

        private void ScreenshotHistory_MouseLeave(object sender, MouseEventArgs e)
        {
            // TODO: fill in
        }

        private void Exit_MouseEnter(object sender, MouseEventArgs e)
        {
            // TODO: fill in
        }

        private void Exit_MouseLeave(object sender, MouseEventArgs e)
        {
            // TODO: fill in
        }

        private void ForceExit_MouseEnter(object sender, MouseEventArgs e)
        {
            // TODO: fill in
        }

        private void ForceExit_MouseLeave(object sender, MouseEventArgs e)
        {
            // TODO: fill in
        }
    }
}