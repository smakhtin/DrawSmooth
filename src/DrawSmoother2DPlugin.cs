using System;
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
	//custom data per graphics device
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
		[Input("")] private IDiffSpread<Vector2D> FXYIn;
		[Input("Insert")] private IDiffSpread<bool> FWriteIn;
		[Input("Width")] private IDiffSpread<int> FLineWidthIn;
		[Input("Smoother")] private IDiffSpread<int> FSmootherIn;
		[Input("Color")] private IDiffSpread<RGBAColor> FColorsIn;
		[Input("SamplingPoints")] private IDiffSpread<int> FPointsRangeIn;
		[Input("Break")] private IDiffSpread<bool> FBreakPointIn;
		[Input("Reset", IsSingle = true)] private IDiffSpread<bool> FResetIn; 
		public static DrawSmoother2D Instance;

		public static int Width = 200;
		public static int Height = 200;

		private readonly LinesList FAllLines = new LinesList();

		readonly List<int> FFrameCount = new List<int>();
		readonly List<bool> FWrite = new List<bool>();
		readonly List<int> FSmoother = new List<int>();
		readonly List<bool> FBreakPoint = new List<bool>();

		readonly List<RGBAColor> FColor = new List<RGBAColor>();

		readonly List<int> FLineWidth = new List<int>();
		readonly List<int> FPointsRange = new List<int>();

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

		#region mainloop
		public void Configurate(IPluginConfig input){}

		public void Evaluate(int spreadMax)
		{
			//Line points count
			if (FFrameCountIn.IsChanged)
			{
				FFrameCount.Clear();
				for (var i = 0; i < FFrameCountIn.SliceCount; i++)
				{
					double a = FFrameCountIn[i];
					if (a < 0) a = 0;
					FFrameCount.Add(Convert.ToInt32(a));
				}

				if (FFrameCount.Count != 0) FAllLines.SetFrameCounts(FFrameCount);
			}

			if (FWriteIn.IsChanged)
			{
				FWrite.Clear();
				for (var i = 0; i < FWriteIn.SliceCount; i++)
				{
					FWrite.Add(FWriteIn[i]);
				}
				
				if (FWrite.Count != 0) FAllLines.SetWrite(FWrite);
			}

			if (FSmootherIn.IsChanged)
			{
				FSmoother.Clear();
				for (var i = 0; i < FSmootherIn.SliceCount; i++)
				{
					var count = FSmootherIn[i];
					count++;
					count = Math.Max(count, 0);
					
					FSmoother.Add(count);
				}

				if (FSmoother.Count != 0) FAllLines.SetSmoothPointsCount(FSmoother);
			}

			if (FBreakPointIn.IsChanged)
			{
				FBreakPoint.Clear();
				for (var i = 0; i < FBreakPointIn.SliceCount; i++)
				{
					FBreakPoint.Add(FBreakPointIn[i]);
				}

				if (FBreakPoint.Count != 0) FAllLines.SetBreakPoints(FBreakPoint);
			}

			if (FColorsIn.IsChanged)
			{
				FColor.Clear();
				
				for (var i = 0; i < FColorsIn.SliceCount; i++)
				{
					FColor.Add(FColorsIn[i]);
				}
				
				if (FColor.Count != 0) FAllLines.SetColors(FColor);
			}

			if (FLineWidthIn.IsChanged)
			{
				FLineWidth.Clear();
				for (var i = 0; i < FLineWidthIn.SliceCount; i++)
				{
					FLineWidth.Add(FLineWidthIn[i]);
				}

				if (FLineWidth.Count != 0) FAllLines.SetLinesWidth(FLineWidth);
			}

			if (FPointsRangeIn.IsChanged)
			{
				FPointsRange.Clear();
				for (var i = 0; i < FPointsRangeIn.SliceCount; i++)
				{
					FPointsRange.Add(FPointsRangeIn[i]);
				}

				if (FPointsRange.Count != 0) FAllLines.SetPointsRange(FPointsRange);
			}

			if (FResetIn.IsChanged)
			{
				if (FResetIn[0]) FAllLines.Reset();
				FMeshes.Clear();
			}

			if (FXYIn.IsChanged)
			{
				var newPoints = new List<Vector2D>();
				
				for (var i = 0; i < FXYIn.SliceCount; i++)
				{
					var x = FXYIn[i].x;
					var y = FXYIn[i].y;
					
					x = VMath.Map(x, -1, 1, 0, Width, TMapMode.Clamp);
					y = VMath.Map(y, -1, 1, 0, Height, TMapMode.Clamp);
					
					newPoints.Add(new Vector2D(x, y));
				}

				if (newPoints.Count != 0)
				{
					FAllLines.NewPoints(newPoints);
				}
			}
		}
		#endregion mainloop

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
				var vS = mesh.LockVertexBuffer(LockFlags.Discard);
				var iS = mesh.LockIndexBuffer(LockFlags.Discard);

				vS.WriteRange(t.Vertices.ToArray());
				iS.WriteRange(t.Indexes.ToArray());

				mesh.UnlockVertexBuffer();
				mesh.UnlockIndexBuffer();

				meshes.Add(mesh);
			}

			foreach (SmoothLine smoothLine in FAllLines.ActualSmoothLines)
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