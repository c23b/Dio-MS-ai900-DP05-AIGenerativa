
using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Media;
using Azure;
using Azure.AI.Vision.ImageAnalysis;

namespace EyeAssist
{
    public partial class MainPage : ContentPage
    {
        string _endpoint;
        string _key;
        public MainPage()
        {
            InitializeComponent();
            CaptureButton.IsEnabled = false; // Desabilita o botão até que a câmera esteja pronta
            _endpoint = "";
            _key = "";
        }
        
        protected override async void OnAppearing()
        {
            base.OnAppearing();

            // Inicializar a câmera
            var status = await Permissions.RequestAsync<Permissions.Camera>();
            if (status == PermissionStatus.Granted)
            {
                CaptureButton.IsEnabled = true; // Habilita o botão após a permissão ser concedida
            }
            else
            {
                await DisplayAlert("Permissão", "Acesso à câmera não permitido.", "OK");
            }
        }
        private void cameraView_CamerasLoaded(object sender, EventArgs e)
        {
            cameraView.Camera = cameraView.Cameras.First();

            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await cameraView.StopCameraAsync();
                await cameraView.StartCameraAsync();
            });
        }
        async void OnCaptureButton(object sender, EventArgs e)
        {
            CaptureButton.IsEnabled = false;
            try
            {
                byte[] imageBytes;
                using (var stream = await cameraView.TakePhotoAsync())
                {
                    var memoryStream = new MemoryStream();
                    await stream.CopyToAsync(memoryStream);
                    imageBytes = memoryStream.ToArray();
                }
                if (!string.IsNullOrEmpty(_endpoint) && !string.IsNullOrEmpty(_key))
                {
                    // Processar a imagem (enviar para a API do Azure Vision Studio)
                    var text = AnalyzeImage(new BinaryData(imageBytes));
                    // Reproduzir o áudio 
                    TextToSpeech.SpeakAsync(text);
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Erro", ex.Message, "OK");
            }
            CaptureButton.IsEnabled = true;
        }
        private string AnalyzeImage(BinaryData image)
        {
            ImageAnalysisClient client = new ImageAnalysisClient(
                new Uri(_endpoint),
                new AzureKeyCredential(_key));

            ImageAnalysisResult result = client.Analyze(
                image,
                VisualFeatures.Caption | VisualFeatures.Read,
                new ImageAnalysisOptions { GenderNeutralCaption = true, Language = null });

            return result.Caption.Text;
        }
    }

}
