using System.ComponentModel;
using System.Reflection;
using JetBrains.Annotations;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using USharpLibs.Common.Utils;
using USharpLibs.Engine.Client;
using USharpLibs.Engine.Client.GL;
using USharpLibs.Engine.Client.UI;
using OpenGL4 = OpenTK.Graphics.OpenGL4.GL;

namespace USharpLibs.Engine {
	[PublicAPI]
	public abstract class GameEngine {
		private static GameEngine instance = default!;

		protected static T Instance<T>() where T : GameEngine => (T)instance;
		private static Lazy<Type> InstanceType { get; } = new(() => instance.GetType());
		public static Lazy<Assembly> InstanceAssembly { get; } = new(() => Assembly.GetAssembly(InstanceType.Value) ?? throw new Exception("Assembly cannot be found."));

		protected internal event Action<EngineWindow>? WindowCreationEvent;
		protected internal event Action<WindowState>? FullscreenToggleEvent;
		protected internal event Func<HashSet<IUnboundShader>>? ShaderCreationEvent;
		protected internal event Func<HashSet<IFont>>? FontCreationEvent;
		protected internal event Func<HashSet<Texture>>? TextureCreationEvent;
		protected internal event Func<HashSet<Screen>>? ScreenCreationEvent;

		private static EngineWindow Window { get; set; } = default!;
		public static LoadState LoadState { get; protected internal set; } = LoadState.NotStarted;
		public static bool IsDebug { get; private set; }
		public static bool CloseRequested { get; internal set; } // I don't like this but GameWindow#IsExiting doesn't seem to work sometimes
		public static Screen? CurrentScreen { get; set; }

		internal static uint RawFPS, RawTPS;
		internal static double RawFrameFrequency, RawTickFrequency;

		public static uint FPS => RawFPS;
		public static uint TPS => RawTPS;
		public static double FrameFrequency => RawFrameFrequency;
		public static double TickFrequency => RawTickFrequency;

		private List<IRenderer> Renderers { get; } = new();
		private HashSet<Screen> Screens { get; } = new();

		public string OriginalTitle { get; }
		protected ushort MaxAmountOfLogs { private get; set; } = 5;

		public static ushort MouseX { get; internal set; }
		public static ushort MouseY { get; internal set; }

		private string title;
		private ushort minWidth;
		private ushort minHeight;
		private ushort maxWidth;
		private ushort maxHeight;

		public string Title {
			get => title;
			protected set {
				title = value;
				Window.Title = title;
			}
		}

		public ushort MinWidth {
			get => minWidth;
			protected set {
				minWidth = value;
				Window.MinimumSize = new(minWidth, MinHeight);
			}
		}

		public ushort MinHeight {
			get => minHeight;
			protected set {
				minHeight = value;
				Window.MinimumSize = new(MinWidth, minHeight);
			}
		}

		public ushort MaxWidth {
			get => maxWidth;
			protected set {
				maxWidth = value;
				Window.MaximumSize = new(maxWidth, MaxHeight);
			}
		}

		public ushort MaxHeight {
			get => maxHeight;
			protected set {
				maxHeight = value;
				Window.MaximumSize = new(MaxWidth, maxHeight);
			}
		}

		protected GameEngine(string title, ushort minWidth, ushort minHeight, ushort maxWidth, ushort maxHeight, bool isDebug = false) {
			OriginalTitle = title;
			this.title = title;
			this.minWidth = minWidth;
			this.minHeight = minHeight;
			this.maxWidth = maxWidth;
			this.maxHeight = maxHeight;
			IsDebug = isDebug;

			Logger.LogLevel = LogLevel.More;
			Logger.SetupDefaultLogFolder(5, $"Starting Client! Today is: {DateTime.Now:d/M/yyyy HH:mm:ss}");
		}

		protected GameEngine(string title, ushort minWidth, ushort minHeight, bool isDebug = false) : this(title, minWidth, minHeight, 0, 0, isDebug) { }
		protected GameEngine(string title, bool isDebug = false) : this(title, 856, 482, 0, 0, isDebug) { }

		public static void Start(GameEngine instance) {
			LoadState = LoadState.PreInit;
			using (Window = new(GameEngine.instance = instance)) {
				LoadState = LoadState.Init;
				instance.Screens.UnionWith(instance.ScreenCreationEvent?.Invoke() ?? new());
				Logger.Debug($"Running Init took {TimeH.Time(instance.Init).Milliseconds}ms");
				Window.Run();
			}

			Logger.Info("Goodbye!");
		}

		internal void OnWindowCreation(EngineWindow window) => WindowCreationEvent?.Invoke(window);

		protected virtual void Init() { }
		protected internal virtual void OnSetupFinished() { }

		internal static void CreateGL() {
			Logger.Info($"Setting up OpenGL! Running OpenGL version: {OpenGL4.GetString(StringName.Version)}");
			OpenGL4.ClearColor(0.1f, 0.1f, 0.1f, 1f);
			OpenGL4.Enable(EnableCap.Blend);
			OpenGL4.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
			GLH.EnableDepthTest();
			GLH.EnableCulling();
		}

		protected internal virtual void SetupGL() {
			AddRenderers(Renderers);

			HashSet<IUnboundShader> shaders = ShaderCreationEvent?.Invoke() ?? new();
			HashSet<IFont> fonts = FontCreationEvent?.Invoke() ?? new();
			HashSet<Texture> textures = TextureCreationEvent?.Invoke() ?? new();

			void Fonts() =>
					fonts.ForEach(f => {
						if (f.Setup() is { } texture) { textures.Add(texture); }
					});

			void Shaders() =>
					shaders.ForEach(s => {
						s.SetupGL();
						Window.Resize += s.OnResize;
					});

			if (fonts.Count != 0) { Logger.Debug($"Setting up {fonts.Count} fonts took {TimeH.Time(Fonts).Milliseconds}ms"); }
			if (textures.Count != 0) { Logger.Debug($"Setting up {textures.Count} textures took {TimeH.Time(() => textures.ForEach(t => t.SetupGL())).Milliseconds}ms"); }
			if (shaders.Count != 0) { Logger.Debug($"Setting up {shaders.Count} shaders took {TimeH.Time(Shaders).Milliseconds}ms"); }
			if (Screens.Count != 0) { Logger.Debug($"Setting up {Screens.Count} screens took {TimeH.Time(() => Screens.ForEach(r => r.SetupGL())).Milliseconds}ms"); }
			if (Renderers.Count != 0) { Logger.Debug($"Setting up {Renderers.Count} renderers took {TimeH.Time(() => Renderers.ForEach(r => r.SetupGL())).Milliseconds}ms"); }
		}

		public virtual void Tick(double time) { }
		public virtual void Render(double time) => Renderers.ForEach(r => r.Render(time));

		protected internal virtual void SetupLoadingScreen() { }
		protected internal virtual void OnResize(ResizeEventArgs e, Vector2i size) => OpenGL4.Viewport(0, 0, size.X, size.Y);

		protected internal virtual void OnKeyPress(KeyboardKeyEventArgs e) { }
		protected internal virtual void OnKeyRelease(KeyboardKeyEventArgs e) { }
		protected internal virtual void OnMouseMove(MouseMoveEventArgs e) { }
		protected internal virtual void OnMousePress(MouseButtonEventArgs e) { }
		protected internal virtual void OnMouseRelease(MouseButtonEventArgs e) { }
		protected internal virtual void OnMouseScroll(MouseWheelEventArgs e) { }
		protected internal virtual void OnClosing(CancelEventArgs e) { }

		protected virtual void AddRenderers(List<IRenderer> renderers) { }

		public void ToggleFullscreen() {
			Window.WindowState = Window.WindowState == WindowState.Normal ? WindowState.Fullscreen : WindowState.Normal;
			FullscreenToggleEvent?.Invoke(Window.WindowState);
		}
	}

	public enum LoadState {
		NotStarted = 0,
		PreInit,
		Init,
		CreateGL,
		SetupGL,
		Done,
	}
}