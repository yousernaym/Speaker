using Microsoft.UI;
using Microsoft.UI.Input;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using System;
using System.Speech.Synthesis;
using Windows.ApplicationModel.DataTransfer;
using Windows.System;
using Windows.UI;
using Windows.UI.Core;

namespace Speaker
{
    public sealed partial class MainWindow : Window
    {
        Brush _playBrush;
        Brush _pauseBrush;
        SpeechSynthesizer _speechSynthesizer;
        bool _updatingProgress;
        int _startPosition;

        bool IsSpeaking => _speechSynthesizer.State == SynthesizerState.Speaking;

        public MainWindow()
        {
            this.InitializeComponent();
            InitSpeech();
            LoadVoices();
            TextInput.Focus(FocusState.Programmatic);
            var accentColor = (Color)Application.Current.Resources["SystemAccentColor"];
            _playBrush = new SolidColorBrush(accentColor);
            _pauseBrush = new SolidColorBrush(Colors.Red);
        }

        private void InitSpeech()
        {
            if (_speechSynthesizer != null)
            {
                _speechSynthesizer.SpeakProgress -= _speechSynthesizer_SpeakProgress;
                _speechSynthesizer.SpeakCompleted -= _speechSynthesizer_SpeakCompleted;
                _speechSynthesizer.Dispose();
            }
            _speechSynthesizer = new SpeechSynthesizer();
            _speechSynthesizer.SpeakProgress += _speechSynthesizer_SpeakProgress;
            _speechSynthesizer.SpeakCompleted += _speechSynthesizer_SpeakCompleted;
        }

        private void LoadVoices()
        {
            foreach (var voice in _speechSynthesizer.GetInstalledVoices())
            {
                VoiceComboBox.Items.Add(voice.VoiceInfo.Name);
            }
            if (VoiceComboBox.Items.Count > 0)
            {
                VoiceComboBox.SelectedIndex = 0;
            }
        }

        private void ToggleSpeakBtn_Click(object sender, RoutedEventArgs e)
        {
            if (IsSpeaking)
                PauseReading();
            else
                StartSpeaking();
        }

        private void StartSpeaking()
        {
            _startPosition = TextInput.Document.Selection.StartPosition;
            InitSpeech();
            SetVoice();
            SetSpeed();

            PlaybackIcon.Symbol = Symbol.Pause;
            ToggleSpeakBtn.Background = _pauseBrush;
            TextInput.Document.GetText(TextGetOptions.UseLf, out var text);
            _speechSynthesizer.SpeakAsync(text.Substring(_startPosition));
        }

        private void PauseReading()
        {
            _speechSynthesizer.Pause();
            PlaybackIcon.Symbol = Symbol.Play;
            ToggleSpeakBtn.Background = _playBrush;
        }

        private void _speechSynthesizer_SpeakProgress(object sender, SpeakProgressEventArgs e)
        {
            _updatingProgress = true;
            HighlightText(e.CharacterPosition + _startPosition, e.CharacterCount);
        }

        private void HighlightText(int charPosition, int charCount)
        {
            TextInput.Document.Selection.SetRange(charPosition, charPosition + charCount);
            TextInput.Focus(FocusState.Programmatic);
        }

        private void _speechSynthesizer_SpeakCompleted(object sender, SpeakCompletedEventArgs e)
        {
            PlaybackIcon.Symbol = Symbol.Play;
            ToggleSpeakBtn.Background = _playBrush;
            TextInput.Document.Selection.SetRange(0, 0);
        }

        private void KeyDown(object sender, KeyRoutedEventArgs e)
        {
            var shiftState = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Shift);
            var controlState = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Control);
            bool isShiftPressed = (shiftState & CoreVirtualKeyStates.Down) == CoreVirtualKeyStates.Down;
            bool isControlPressed = (controlState & CoreVirtualKeyStates.Down) == CoreVirtualKeyStates.Down;

            if (e.Key == VirtualKey.Space)
            {
                ToggleSpeakBtn_Click(this, null);
                e.Handled = true;
            }
            else if (e.Key == VirtualKey.Left)
            {
                var prevWordPos = FindStartOfPreviousWord();
                TextInput.Document.Selection.SetRange(prevWordPos, prevWordPos);
                e.Handled = true;
            }
            else if (e.Key == VirtualKey.Right)
            {
                var nextWordPos = FindStartOfNextWord();
                TextInput.Document.Selection.SetRange(nextWordPos, nextWordPos);
                e.Handled = true;
            }
            else if (e.Key == VirtualKey.Up && isControlPressed)
            {
                var pos = FindStartOfPreviousParagraph();
                TextInput.Document.Selection.SetRange(pos, pos);
                e.Handled = true;
            }
            else if (e.Key == VirtualKey.Up)
            {
                TextInput.Document.GetText(TextGetOptions.UseLf, out var text);
                var position = TextInput.Document.Selection.StartPosition;
                if (position > 0 && text[position - 1] == '\n')
                {
                    var prevWordPos = FindStartOfPreviousWord();
                    TextInput.Document.Selection.SetRange(prevWordPos, prevWordPos);
                    e.Handled = true;
                }
            }
        }

        int FindStartOfPreviousWord()
        {
            var position = TextInput.Document.Selection.StartPosition;
            TextInput.Document.GetText(TextGetOptions.UseLf, out var text);
            if (text.Length == 0)
                return 0;
            if (position >= text.Length)
                position = text.Length - 1;
            while (position > 0 && !char.IsWhiteSpace(text[position]))
                position--;
            while (position > 0 && char.IsWhiteSpace(text[position]))
                position--;
            while (position > 0 && !char.IsWhiteSpace(text[position]))
                position--;

            return position;
        }

        int FindStartOfNextWord()
        {
            var position = TextInput.Document.Selection.StartPosition;
            TextInput.Document.GetText(Microsoft.UI.Text.TextGetOptions.UseLf, out var text);
            if (text.Length == 0)
                return 0;
            if (position >= text.Length)
                return text.Length - 1;
            while (position < text.Length && !char.IsWhiteSpace(text[position]))
                position++;
            while (position < text.Length && char.IsWhiteSpace(text[position]))
                position++;

            return position;
        }

        void HighlightWordAt(int position)
        {
            TextInput.Document.GetText(Microsoft.UI.Text.TextGetOptions.UseLf, out var text);
            if (position >= text.Length)
            {
                TextInput.Document.Selection.SetRange(text.Length, text.Length);
                return;
            }

            var start = position;
            // If we start on whitespace, move forward to next word.
            if (char.IsWhiteSpace(text[start]))
            {
                while (start < text.Length && char.IsWhiteSpace(text[start]))
                    start++;
            }
            else
            {
                while (start > 0 && !char.IsWhiteSpace(text[start]))
                    start--;
                if (char.IsWhiteSpace(text[start]))
                    start++;
            }
            var end = start;
            while (end < text.Length && !char.IsWhiteSpace(text[end]))
                end++;
            HighlightText(start, end - start);
        }



        private void InputTextBox_TextChanged(object sender, RoutedEventArgs e)
        {
            StartSpeaking();
        }

        private void InputTextBox_SelectionChanged(object sender, RoutedEventArgs e)
        {
            if (!_updatingProgress && IsSpeaking)
                StartSpeaking();
            if (!_updatingProgress)
                HighlightWordAt(TextInput.Document.Selection.StartPosition);
            else
                _updatingProgress = false;
        }

        private void SpeedSlider_ValueChanged(object sender, Microsoft.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
        {
            if (IsSpeaking)
                StartSpeaking();
        }

        private void SetSpeed()
        {
            _speechSynthesizer.Rate = (int)SpeedSlider.Value;
        }

        private void VoiceComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (IsSpeaking)
                StartSpeaking();
        }

        private void SetVoice()
        {
            var voice = VoiceComboBox.SelectedItem.ToString();
            _speechSynthesizer.SelectVoice(voice);
        }

        private async void Paste_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs e)
        {
            var text = await Clipboard.GetContent().GetTextAsync();
            TextInput.IsReadOnly = false;
            TextInput.Document.SetText(TextSetOptions.None, text);
            TextInput.IsReadOnly = true;
            e.Handled = true;
        }

        int FindStartOfPreviousParagraph()
        {
            var position = TextInput.Document.Selection.StartPosition;
            TextInput.Document.GetText(TextGetOptions.UseLf, out var text);
            if (position >= text.Length)
                return text.Length - 1;

            while (position > 0 && text[position] != '\n')
                position--;
            while (position > 0 && char.IsWhiteSpace(text[position]))
                position--;
            while (position > 0 && text[position] != '\n')
                position--;

            return position;
        }

        private void Window_Closed(object sender, WindowEventArgs args)
        {
            _speechSynthesizer?.Dispose();
        }

        //bool IsLineBreakAtPosition(int position)
        //{
        //    if (position >= InputTextBox.Document.leng - 1)
        //        return true;

        //    var rect1 = InputTextBox.GetRectFromCharacterIndex(position, false);
        //    var rect2 = InputTextBox.GetRectFromCharacterIndex(position + 1, false);

        //    // If the Y-coordinate of the next character is greater, it's on a new line
        //    return rect2.Y > rect1.Y;
        //}

    }
}
