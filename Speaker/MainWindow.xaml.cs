using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using System;
using System.Linq;
using System.Speech.Synthesis;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.UI.StartScreen;
using Windows.UI.Text.Core;
using static System.Net.Mime.MediaTypeNames;

namespace Speaker
{
    public sealed partial class MainWindow : Window
    {
        SpeechSynthesizer _speechSynthesizer;
        bool _updatingProgress;
        int _startPosition;
        
        bool IsSpeaking => _speechSynthesizer.State == SynthesizerState.Speaking;

        public MainWindow()
        {
            this.InitializeComponent();
            InitSpeech();
            LoadVoices();
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

        private void TogglePlayPauseButton_Click(object sender, RoutedEventArgs e)
        {
            if (IsSpeaking)
                PauseReading();
            else
                StartSpeaking();
        }

        private void StartSpeaking()
        {
            _startPosition = InputTextBox.SelectionStart;
            //if (_startPosition == InputTextBox.Text.Length)
            //    _startPosition = 0;
            InitSpeech();
            SetVoice();
            SetSpeed();

            PlaybackIcon.Symbol = Symbol.Pause;
            _speechSynthesizer.SpeakAsync(InputTextBox.Text.Substring(_startPosition));
        }

        private void PauseReading()
        {
            _speechSynthesizer.Pause();
            PlaybackIcon.Symbol = Symbol.Play;
        }

        private void _speechSynthesizer_SpeakProgress(object sender, SpeakProgressEventArgs e)
        {
            HighlightCurrentWord(e.CharacterPosition + _startPosition, e.CharacterCount);
        }

        private void HighlightCurrentWord(int charPosition, int charCount)
        {
            _updatingProgress = true;
            InputTextBox.Select(charPosition, charCount);
            //InputTextBox.Focus(FocusState.Programmatic);
        }

        private void _speechSynthesizer_SpeakCompleted(object sender, SpeakCompletedEventArgs e)
        {
            PlaybackIcon.Symbol = Symbol.Play;
            InputTextBox.Select(0, 0);
        }

        private void InputTextBox_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Space)
            {
                TogglePlayPauseButton_Click(this, null);
            }
        }

        private void InputTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (IsSpeaking)
                StartSpeaking();
        }

        private void InputTextBox_SelectionChanged(object sender, RoutedEventArgs e)
        {
            if (!_updatingProgress && IsSpeaking)
                StartSpeaking();
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

        private async void Paste_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            InputTextBox.Text = await Clipboard.GetContent().GetTextAsync();
            args.Handled = true;
        }
    }
}
