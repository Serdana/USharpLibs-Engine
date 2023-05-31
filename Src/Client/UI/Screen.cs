using JetBrains.Annotations;
using OpenTK.Windowing.GraphicsLibraryFramework;
using USharpLibs.Common.Utils;
using USharpLibs.Engine.Client.GL;
using USharpLibs.Engine.Client.UI.Elements;
using USharpLibs.Engine.Init;

namespace USharpLibs.Engine.Client.UI {
	[PublicAPI]
	public abstract class Screen {
		protected Dictionary<string, UiElement> Elements { get; } = new();
		private HoverableUiElement? currentlyHovered;
		private FocusableUiElement? currentlyFocused;
		private ClickableUiElement? currentlyPressed;

		protected void AddElement(string key, UiElement e, bool replace = false) {
			if (GameEngine.LoadState != LoadState.Init) {
				Logger.Warn($"Cannot add UiElement during {GameEngine.LoadState}");
				return;
			}

			if (Elements.ContainsKey(key)) {
				if (replace) { Elements.Add(key, e); } else { Logger.Warn($"Duplicate key '{key}' found. If this was intentional set 'replace' to true"); }
				return;
			}

			Elements.Add(key, e);
		}

		internal void SetupGL() {
			if (GameEngine.LoadState != LoadState.SetupGL) { throw new Exception($"Cannot setup Screen OpenGL during {GameEngine.LoadState}"); }
			ISetupGL();
		}

		protected virtual void ISetupGL() {
			foreach (UiElement e in Elements.Values) { e.SetupGL(); }
		}

		public virtual void Render(double time) {
			GLH.Bind(DefaultShaders.DefaultHud, s => {
				foreach (UiElement e in Elements.Values) {
					if (e.IsEnabled) {
						s.SetVector3("Position", new(e.X, e.Y, e.Z));
						e.Render(s, time);
					}
				}

				s.SetVector3("Position", new());
			});
		}

		internal bool CheckForPress(MouseButton button, ushort mouseX, ushort mouseY) {
			foreach (UiElement element in Elements.Values) {
				if (element is ClickableUiElement e && e.CheckForPress(mouseX, mouseY)) {
					if (e == currentlyPressed) { return true; }

					if (e.OnPress(button)) {
						currentlyPressed = e;
						return true;
					}
				}
			}

			currentlyPressed = null;
			return false;
		}

		internal bool CheckForRelease(MouseButton button, ushort mouseX, ushort mouseY) {
			if ((currentlyPressed?.CheckForRelease(mouseX, mouseY) ?? false) && (currentlyPressed?.OnRelease(button) ?? false)) {
				currentlyPressed = null;
				return true;
			}

			currentlyPressed?.OnReleaseFailed(button);
			currentlyPressed = null;
			return false;
		}

		internal void CheckForFocus(ushort mouseX, ushort mouseY) {
			foreach (UiElement element in Elements.Values) {
				if (element is FocusableUiElement e && e.CheckForFocus(mouseX, mouseY)) {
					if (e == currentlyFocused) { return; }

					currentlyFocused?.InvokeFocusLost();
					currentlyFocused = e;
					currentlyFocused.InvokeFocusGain();
					return;
				}
			}

			currentlyFocused?.InvokeFocusLost();
			currentlyFocused = null;
		}

		internal void CheckForHover(ushort mouseX, ushort mouseY) {
			foreach (UiElement element in Elements.Values) {
				if (element is HoverableUiElement e && e.CheckForHover(mouseX, mouseY)) {
					if (e == currentlyHovered) { return; }

					currentlyHovered?.InvokeHoverLost();
					currentlyHovered = e;
					currentlyHovered.InvokeHoverGain();
					return;
				}
			}

			currentlyHovered?.InvokeHoverLost();
			currentlyHovered = null;
		}
	}
}