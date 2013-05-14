using System.Collections.Generic;
using System.ComponentModel.Composition;
using VVVV.PluginInterfaces.V2;
using VVVV.PluginInterfaces.V2.EX9;
using VVVV.Utils.VColor;
using VVVV.Utils.VMath;
using VVVV.PluginInterfaces.V1;
using VVVV.Lib;
using SlimDX;
using SlimDX.Direct3D9;

namespace VVVV.Nodes
{
	public class CustomDeviceData : DeviceData
	{
		public Device Device { get; private set; }

		public CustomDeviceData(Device device)
		{
			Device = device;
		}
	}

	[PluginInfo(Name = "DrawSmooth", Author = "bo27", Credits = "alg", Category = "DX9", Help = "Draws smooth lines")]
	public class DrawSmoother2D : DXLayerOutPluginBase<CustomDeviceData>, IPluginEvaluate
	{
		[Input("FrameCount", DefaultValue = 20, Visibility = PinVisibility.Hidden)] private IDiffSpread<int> FFrameCountIn;
		[Input("Transform")] private ISpread<Matrix> FWorldTransformIn;
		[Input("")] private ISpread<Vector2D> FXYIn;
		[Input("Insert")] private ISpread<bool> FWriteIn;
		[Input("Width")] private ISpread<int> FLineWidthIn;
		[Input("Smoother")] private ISpread<int> FSmootherIn;
		[Input("Color")] private ISpread<RGBAColor> FColorsIn;
		[Input("SamplingPoints")] private ISpread<int> FPointsRangeIn;
		[Input("Break")] private ISpread<bool> FBreakPointIn;
		[Input("Reset", IsSingle = true)] private ISpread<bool> FResetIn;
		public static DrawSmoother2D Instance;

		public static int Width = 200;
		public static int Height = 200;

		private readonly LinesList FAllLines = new LinesList();

		private readonly Dictionary<Device, Mesh> FMeshes = new Dictionary<Device, Mesh>();

		public IPluginHost Host;

		//output pin declaration
		private IDXLayerIO FLayerOutput;

		[ImportingConstructor]
		public DrawSmoother2D(IPluginHost host)
			: base(host, true, true)
		{
			SetPluginHost(host);
		}

		//this method is called by vvvv when the node is created
		public void SetPluginHost(IPluginHost host)
		{
			//assign host
			Host = host;
			Instance = this;

			//create outputs	    
			Host.CreateLayerOutput("Layer", TPinVisibility.True, out FLayerOutput);
		}

		#region DXLayer
		protected override void Render(Device device, CustomDeviceData deviceData)
		{
			FRenderStatePin.SetSliceStates(0);
			device.SetTransform(TransformState.World, FWorldTransformIn[0]);

			Width = device.Viewport.Width;
			Height = device.Viewport.Height;

			device.VertexFormat = (VertexFormat)Microsoft.DirectX.Direct3D.CustomVertex.PositionColoredTextured.Format;

			foreach (var line in FAllLines.ActualSmoothLines)
			{
				lock (line.TriangleList)
				{
					foreach (var triangle in line.TriangleList)
					{
						device.DrawUserPrimitives(PrimitiveType.TriangleList, 1, triangle.Vertices);
					}
				}
			}
		}

		public override void SetStates()
		{
			FRenderStatePin.SetRenderState(RenderState.AlphaTestEnable, 1);
			FRenderStatePin.SetRenderState(RenderState.SourceBlend, 1);
			FRenderStatePin.SetRenderState(RenderState.DestinationBlend, 1);
		}
		#endregion

		public void Configurate(IPluginConfig input){}

		public void Evaluate(int spreadMax)
		{
			//TODO: Proper set methods refactor
			FAllLines.SetFrameCounts(FFrameCountIn);
			FAllLines.SetWrite(FWriteIn);
			FAllLines.SetSmoothPointsCount(FSmootherIn);
			FAllLines.SetBreakPoints(FBreakPointIn);
			FAllLines.SetColors(FColorsIn);
			FAllLines.SetLinesWidth(FLineWidthIn);
			FAllLines.SetPointsRange(FPointsRangeIn);
			
			if (FResetIn[0])
			{
				FAllLines.Reset();
				FMeshes.Clear();
			}
			
			var newPoints = new Spread<Vector2D>();
				
			for (var i = 0; i < FXYIn.SliceCount; i++)
			{
				var x = FXYIn[i].x;
				var y = FXYIn[i].y;
					
				x = VMath.Map(x, -1, 1, 0, Width, TMapMode.Float);
				y = VMath.Map(y, -1, 1, 0, Height, TMapMode.Float);
					
				newPoints.Add(new Vector2D(x, y));
			}

			FAllLines.NewPoints(newPoints);
		}

		protected override CustomDeviceData CreateDeviceData(Device device)
		{
			return new CustomDeviceData(device);
		}

		protected override void UpdateDeviceData(CustomDeviceData deviceData)
		{
			var device = deviceData.Device;

			var meshes = new List<Mesh>();

			foreach (var t in FAllLines.BreakedSmoothLines)
			{
				if (t.Indexes.Count == 0 || t.Vertices.Count == 0) continue;

				var mesh = new Mesh(device, t.Indexes.Count / 3,
									 t.Vertices.Count, MeshFlags.Dynamic | MeshFlags.WriteOnly, Vertex.Format);
				var vertexes = mesh.LockVertexBuffer(LockFlags.Discard);
				var indexes = mesh.LockIndexBuffer(LockFlags.Discard);

				vertexes.WriteRange(t.Vertices.ToArray());
				indexes.WriteRange(t.Indexes.ToArray());

				mesh.UnlockVertexBuffer();
				mesh.UnlockIndexBuffer();

				meshes.Add(mesh);
			}

			foreach (var smoothLine in FAllLines.ActualSmoothLines)
			{
				if (smoothLine.Indexes.Count == 0 || smoothLine.Vertices.Count == 0) continue;

				var mesh = new Mesh(device, smoothLine.Indexes.Count / 3,
				                     smoothLine.Vertices.Count, MeshFlags.Dynamic | MeshFlags.WriteOnly, Vertex.Format);
				var vertexes = mesh.LockVertexBuffer(LockFlags.Discard);
				var indexes = mesh.LockIndexBuffer(LockFlags.Discard);

				vertexes.WriteRange(smoothLine.Vertices.ToArray());
				indexes.WriteRange(smoothLine.Indexes.ToArray());

				mesh.UnlockVertexBuffer();
				mesh.UnlockIndexBuffer();

				meshes.Add(mesh);
			}

			if (meshes.Count != 0)
			{
				var merge = Mesh.Concatenate(device, meshes.ToArray(), MeshFlags.Use32Bit | MeshFlags.Managed);
				FMeshes.Add(deviceData.Device, merge);
			}

			foreach (var m in meshes)
			{
				m.Dispose();
			}
		}

		protected override void DestroyDeviceData(CustomDeviceData deviceData, bool onlyUnManaged)
		{
			var device = deviceData.Device;

			if (!FMeshes.ContainsKey(device)) return;
			
			var mesh = FMeshes[device];
			FMeshes.Remove(device);
			mesh.Dispose();
		}
	}
}