using System.Reflection;
using JetBrains.Annotations;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using USharpLibs.Common.IO;
using USharpLibs.Engine.Utils;
using OpenGL4 = OpenTK.Graphics.OpenGL4.GL;

namespace USharpLibs.Engine.Client.GL {
	[PublicAPI]
	public abstract class Shader {
		public const uint PositionLocation = 0, TextureLocation = 1;
		public Assembly? AssemblyOverride { get; init; }

		protected Dictionary<string, int> UniformLocations { get; } = new();
		protected Dictionary<string, object?> LastValues { get; } = new();

		public int Handle { get; protected set; }
		protected string VertName { get; }
		protected string FragName { get; }
		public bool WasSetup { get; private set; }

		protected Shader(string vertName, string fragName) {
			VertName = vertName;
			FragName = fragName;
		}

		internal void SetupGL() {
			if (GameEngine.CurrentLoadState != GameEngine.LoadState.SetupGL) { throw new($"Cannot setup shader during {GameEngine.CurrentLoadState}"); }
			ISetupGL();
			WasSetup = true;
		}

		protected abstract void ISetupGL();
		protected internal virtual void OnResize(ResizeEventArgs args) { }

		protected void CompileShader(ShaderType type, string name, out int shader) {
			Assembly assembly = AssemblyOverride ?? GameEngine.InstanceAssembly.Value;
			string streamName = $"{assembly.GetName().Name}.Assets.Shaders.{name}.{type.ToFileFormat()}", result;

			if (assembly.GetManifestResourceStream(streamName) is { } stream) {
				using (stream)
				using (StreamReader reader = new(stream)) { result = reader.ReadToEnd(); }
			} else { throw new($"Could not find file '{name}' at '{streamName}'"); }

			OpenGL4.ShaderSource(shader = OpenGL4.CreateShader(type), result);
			OpenGL4.CompileShader(shader);
			OpenGL4.GetShader(shader, ShaderParameter.CompileStatus, out int code);
			if (code != (int)All.True) { throw new($"Error occurred whilst compiling Shader({shader}).\n\n{OpenGL4.GetShaderInfoLog(shader)}"); }
		}

		protected void SetData<V>(string name, V data, Action<int, V> apply) {
			if (GLH.CurrentShader != Handle && GameEngine.CurrentLoadState != GameEngine.LoadState.SetupGL) {
				Logger.Warn("Trying to use an unbound shader!");
				return;
			} else if (!UniformLocations.ContainsKey(name)) {
				Logger.Warn($"Tried to set variable named '{name}' in shader '{VertName}/{FragName}' but it doesn't exist!");
				return;
			}

			apply(UniformLocations[name], data);
			LastValues[name] = data;
		}

		protected void SetMatrix(string name, bool flag, Matrix4 data) {
			if (GLH.CurrentShader != Handle && GameEngine.CurrentLoadState != GameEngine.LoadState.SetupGL) {
				Logger.Warn("Trying to use an unbound shader!");
				return;
			} else if (!UniformLocations.ContainsKey(name)) {
				Logger.Warn($"Tried to set variable named '{name}' in shader '{VertName}/{FragName}' but it doesn't exist!");
				return;
			}

			OpenGL4.UniformMatrix4(UniformLocations[name], flag, ref data);
			LastValues[name] = data;
		}

		protected void SetMatrix4Array(string name, bool flag, Matrix4[] datas) {
			if (GLH.CurrentShader != Handle && GameEngine.CurrentLoadState != GameEngine.LoadState.SetupGL) {
				Logger.Warn("Trying to use an unbound shader!");
				return;
			} else if (!UniformLocations.ContainsKey(name)) {
				Logger.Warn($"Tried to set variable named '{name}' in shader '{VertName}/{FragName}' but it doesn't exist!");
				return;
			}

			OpenGL4.UniformMatrix4(UniformLocations[name], datas.Length, flag, ref datas[0].Row0.X);
			LastValues[name] = datas;
		}

		public void SetBool(string name, bool data) => SetData(name, data ? 1 : 0, OpenGL4.Uniform1);
		public void SetInt(string name, int data) => SetData(name, data, OpenGL4.Uniform1);
		public void SetFloat(string name, float data) => SetData(name, data, OpenGL4.Uniform1);
		public void SetVector2(string name, Vector2 data) => SetData(name, data, OpenGL4.Uniform2);
		public void SetVector3(string name, Vector3 data) => SetData(name, data, OpenGL4.Uniform3);
		public void SetVector4(string name, Vector4 data) => SetData(name, data, OpenGL4.Uniform4);
		public void SetMatrix4(string name, Matrix4 data) => SetMatrix(name, true, data);
		public void SetMatrix4Array(string name, Matrix4[] data) => SetMatrix4Array(name, true, data);
		public void SetColor(string name, Color4 data) => SetData(name, data, OpenGL4.Uniform4);

		public T? GetLastValue<T>(string name) where T : notnull {
			LastValues.TryGetValue(name, out object? value);
			return (T?)value;
		}
	}
}