using Microsoft.UI;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using System;
using System.Linq;
using System.Speech.Synthesis;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.System;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.StartScreen;
using Windows.UI.Text.Core;

namespace Speaker
{
    public sealed partial class MainWindow : Window
    {
        Brush _playBrush;
        Brush _pauseBrush;
        SpeechSynthesizer _speechSynthesizer;
        bool _updatingProgress;
        int _startPosition;
        string _inputText;

        bool IsSpeaking => _speechSynthesizer.State == SynthesizerState.Speaking;

        public MainWindow()
        {
            this.InitializeComponent();
            InitSpeech();
            LoadVoices();
            InputTextBox.Focus(FocusState.Programmatic);
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
            _startPosition = InputTextBox.SelectionStart;
            InitSpeech();
            SetVoice();
            SetSpeed();

            PlaybackIcon.Symbol = Symbol.Pause;
            ToggleSpeakBtn.Background = _pauseBrush;
            _speechSynthesizer.SpeakAsync(InputTextBox.Text.Substring(_startPosition));
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
            InputTextBox.Select(charPosition, charCount);
            InputTextBox.Focus(FocusState.Programmatic);
        }

        private void _speechSynthesizer_SpeakCompleted(object sender, SpeakCompletedEventArgs e)
        {
            PlaybackIcon.Symbol = Symbol.Play;
            ToggleSpeakBtn.Background = _playBrush;
            InputTextBox.Select(0, 0);
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
                InputTextBox.Select(prevWordPos, 0);
                e.Handled = true;
            }
            else if (e.Key == VirtualKey.Right)
            {
                var nextWordPos = FindStartOfNextWord();
                InputTextBox.Select(nextWordPos, 0);
                e.Handled = true;
            }
            //else if (e.Key == VirtualKey.Up && isShiftPressed && isControlPressed)
            //{
            //    var pos = FindStartOfPreviousParagraph();
            //    InputTextBox.Select(pos, 0);
            //    e.Handled = true;
            //}
        }

        int FindStartOfPreviousWord()
        {
            var position = InputTextBox.SelectionStart;
            var text = InputTextBox.Text;
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
            var position = InputTextBox.SelectionStart;
            var text = InputTextBox.Text;
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
            var text = InputTextBox.Text;
            if (position >= text.Length)
            {
                InputTextBox.Select(text.Length, 0);
                return;
            }

            var start = position;
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



        private void InputTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            StartSpeaking();
        }

        private void InputTextBox_SelectionChanged(object sender, RoutedEventArgs e)
        {
            if (!_updatingProgress && IsSpeaking)
                StartSpeaking();
            if (!_updatingProgress)
                HighlightWordAt(InputTextBox.SelectionStart);
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
            InputTextBox.Text = _inputText = await Clipboard.GetContent().GetTextAsync();
            e.Handled = true;
        }

        int FindStartOfPreviousParagraph()
        {
            var position = InputTextBox.SelectionStart;

            if (position >= _inputText.Length)
                return _inputText.Length - 1;

            while (position > 0 && _inputText[position] != '\n')
                position--;
            while (position > 0 && char.IsWhiteSpace(_inputText[position]))
                position--;
            while (position > 0 && _inputText[position] != '\n')
                position--;

            return position;
        }

        bool IsLineBreakAtPosition(int position)
        {
            if (position >= InputTextBox.Text.Length - 1)
                return true;

            var rect1 = InputTextBox.GetRectFromCharacterIndex(position, false);
            var rect2 = InputTextBox.GetRectFromCharacterIndex(position + 1, false);

            // If the Y-coordinate of the next character is greater, it's on a new line
            return rect2.Y > rect1.Y;
        }

    }
}
