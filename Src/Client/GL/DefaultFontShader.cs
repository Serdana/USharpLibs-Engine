using JetBrains.Annotations;
using OpenTK.Mathematics;

namespace USharpLibs.Engine.Client.GL {
	[PublicAPI]
	public class DefaultFontShader : DefaultHudShader {
		public const bool DefaultDrawFont = true;
		public const bool DefaultDrawOutline = true;
		public const byte DefaultOutlineSize = 150;

		public static Color4 DefaultFontColor => Color4.White;
		public static Color4 DefaultOutlineColor => Color4.Black;

		public DefaultFontShader(string vertName, string fragName) : base(vertName, fragName) { }
		public DefaultFontShader(string name) : base(name) { }

		public void SetFontColor() => SetFontColor(DefaultFontColor);
		public void SetOutlineColor() => SetOutlineColor(DefaultOutlineColor);

		public void SetFontColor(Color4 color) => SetColor("FontColor", color);
		public void SetOutlineColor(Color4 color) => SetColor("OutlineColor", color);

		public void SetDrawFont(bool value = DefaultDrawFont) => SetBool("DrawFont", value);
		public void SetDrawOutline(bool value = DefaultDrawOutline) => SetBool("DrawOutline", value);

		public void SetOutlineSize(byte value = DefaultOutlineSize) => SetInt("OutlineSize", value);
	}
}