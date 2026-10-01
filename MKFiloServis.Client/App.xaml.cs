using Microsoft.Extensions.DependencyInjection;

namespace MKFiloServis.Client;

public partial class App : Application
{
	public App()
	{
		InitializeComponent();
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
		return DeviceInfo.Platform == DevicePlatform.WinUI
			? new Window(new MainPage()) { Title = "MK Filo Servis" }
			: new Window(new AppShell());
	}
}
