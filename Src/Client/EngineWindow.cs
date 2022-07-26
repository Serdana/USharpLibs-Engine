using OpenTK.Graphics.OpenGL4;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenGL4 = OpenTK.Graphics.OpenGL4.GL;

namespace USharpLibs.Engine.Client {
	public class EngineWindow : GameWindow {
		private readonly ClientBase client;
		public string BaseTitle { get; private set; }

		private uint frameCounter, tickCounter;
		private double frameTimeCounter, tickTimeCounter;

		public EngineWindow(ClientBase client, ushort minWidth, ushort minHeight) : base(GameWindowSettings.Default, new NativeWindowSettings {
			MinimumSize = new(minWidth, minHeight),
			Size = new(minWidth, minHeight),
			Title = client.Title,
		}) {
			this.client = client;
			BaseTitle = client.Title;
			UpdateFrequency = 60;

			Load += () => {
				ClientBase.LoadState = LoadState.GL;
				client.SetupGL();
				ClientBase.LoadState = LoadState.Done;
				client.OnSetupFinished();
			};
			Resize += e => client.OnResize(e, Size);
			KeyDown += client.OnKeyPress;
			KeyUp += client.OnKeyRelease;
			MouseMove += client.OnMouseMove;
			MouseDown += client.OnMousePress;
			MouseUp += client.OnMouseRelease;
			MouseWheel += client.OnMouseScroll;
			Closing += client.OnClosing;
		}

		public virtual void ToggleFullscreen() {
			WindowState = WindowState == WindowState.Normal ? WindowState.Fullscreen : WindowState.Normal;
			client.OnFullscreenToggle(WindowState);
		}

		protected override void OnRenderFrame(FrameEventArgs args) {
			Calc(args.Time, ref frameCounter, ref frameTimeCounter, ref ClientBase.RawFrameFrequency, ref ClientBase.RawFPS);

			OpenGL4.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
			client.Render(args.Time);
			SwapBuffers();
		}

		protected override void OnUpdateFrame(FrameEventArgs args) {
			Calc(args.Time, ref tickCounter, ref tickTimeCounter, ref ClientBase.RawTickFrequency, ref ClientBase.RawTPS);

			client.Tick(args.Time);
		}

		private static void Calc(double time, ref uint counter, ref double timeCounter, ref double frequency, ref uint result) {
			frequency = time * 1000;
			counter++;

			if ((timeCounter += time) >= 1) {
				result = counter;
				counter = 0;
				timeCounter -= 1;
			}
		}
	}
}