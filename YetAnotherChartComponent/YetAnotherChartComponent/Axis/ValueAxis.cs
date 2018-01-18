﻿using eScape.Core;
using System;
using System.Collections.Generic;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;

namespace eScapeLLC.UWP.Charts {
	#region ValueAxis
	/// <summary>
	/// Value axis is a "vertical" axis that represents the "Y" coordinate.
	/// </summary>
	public class ValueAxis : AxisCommon, IRequireLayout, IRequireRender, IRequireTransforms, IRequireEnterLeave {
		static LogTools.Flag _trace = LogTools.Add("ValueAxis", LogTools.Level.Error);
		#region properties
		/// <summary>
		/// Path for the axis "bar".
		/// </summary>
		protected Path Axis { get; set; }
		/// <summary>
		/// Geometry for the axis bar.
		/// </summary>
		protected PathGeometry AxisGeometry { get; set; }
		/// <summary>
		/// List of active TextBlocks for labels.
		/// </summary>
		protected List<TextBlock> TickLabels { get; set; }
		/// <summary>
		/// The layer to manage components.
		/// </summary>
		protected IChartLayer Layer { get; set; }
		#endregion
		#region ctor
		/// <summary>
		/// Default ctor.
		/// Creates Value/Left/Vertical axis.
		/// </summary>
		public ValueAxis() : base(AxisType.Value, AxisOrientation.Vertical, Side.Left) {
			CommonInit();
		}
		#endregion
		#region helpers
		private void CommonInit() {
			TickLabels = new List<TextBlock>();
			Axis = new Path();
			AxisGeometry = new PathGeometry();
			Axis.Data = AxisGeometry;
			MinWidth = 32;
		}
		void DoBindings(IChartEnterLeaveContext icelc) {
			BindTo(this, "PathStyle", Axis, Path.StyleProperty);
		}
		void DoTickLabels(IChartRenderContext icrc) {
			var tc = new TickCalculator(Minimum, Maximum);
			_trace.Verbose($"grid range:{tc.Range} tintv:{tc.TickInterval}");
			var padding = AxisLineThickness + 2 * AxisMargin;
			var tbr = new Recycler<TextBlock>(TickLabels, () => {
				if (LabelStyle != null) {
					// let style override everything but what MUST be calculated
					var tb = new TextBlock() {
						Width = icrc.Area.Width - padding,
						Padding = Side == Side.Right ? new Thickness(padding, 0, 0, 0) : new Thickness(0, 0, padding, 0)
					};
					tb.Style = LabelStyle;
					return tb;
				} else {
					// SHOULD NOT execute this code, unless default style failed!
					var tb = new TextBlock() {
						FontSize = 10,
						Foreground = Axis.Fill,
						VerticalAlignment = VerticalAlignment.Center,
						HorizontalAlignment = Side == Side.Right ? HorizontalAlignment.Left : HorizontalAlignment.Right,
						Width = icrc.Area.Width - padding,
						TextAlignment = Side == Side.Right ? TextAlignment.Left : TextAlignment.Right,
						Padding = Side == Side.Right ? new Thickness(padding, 0, 0, 0) : new Thickness(0, 0, padding, 0)
					};
					return tb;
				}
			});
			var tbget = tbr.Items().GetEnumerator();
			foreach (var tick in tc.GetTicks()) {
				//_trace.Verbose($"grid vx:{tick}");
				if (tbget.MoveNext()) {
					var tb = tbget.Current;
					tb.Text = tick.ToString(String.IsNullOrEmpty(LabelFormatString) ? "G" : LabelFormatString);
					tb.SetValue(Canvas.LeftProperty, icrc.Area.Left);
					tb.SetValue(Canvas.TopProperty, tick);
					// cheat: save the grid value so we can rescale the Canvas.Top in Transforms()
					tb.Tag = tick;
				}
			}
			// VT and internal bookkeeping
			Layer.Remove(tbr.Unused);
			Layer.Add(tbr.Created);
			foreach (var tb in tbr.Unused) {
				TickLabels.Remove(tb);
			}
			TickLabels.AddRange(tbr.Created);
		}
		#endregion
		#region extensions
		/// <summary>
		/// Add elements and attach bindings.
		/// </summary>
		/// <param name="icelc">The context.</param>
		void IRequireEnterLeave.Enter(IChartEnterLeaveContext icelc) {
			Layer = icelc.CreateLayer(Axis);
			DoBindings(icelc);
			ApplyLabelStyle();
			if (PathStyle == null && Theme != null) {
				if (Theme.PathAxisCategory != null) PathStyle = Theme.PathAxisCategory;
			}
		}
		/// <summary>
		/// Reverse effect of Enter.
		/// </summary>
		/// <param name="icelc">The context.</param>
		void IRequireEnterLeave.Leave(IChartEnterLeaveContext icelc) {
			icelc.DeleteLayer(Layer);
			Layer = null;
		}
		/// <summary>
		/// Claim the space indicated by properties.
		/// </summary>
		/// <param name="iclc"></param>
		void IRequireLayout.Layout(IChartLayoutContext iclc) {
			var space = AxisMargin + AxisLineThickness + MinWidth;
			iclc.ClaimSpace(this, Side, space);
		}
		/// <summary>
		/// Layout axis components (bar, grid, labels).
		/// Each component has a corresponding transform (applied in Transforms()).  Right and Left are DUALs of each other wrt to horizontal axis.
		/// Axis "bar" and Tick marks:
		///		x: PX (scale 1)
		///		y: "axis" scale
		/// Tick labels:
		///		x, y: PX
		/// </summary>
		/// <param name="icrc"></param>
		void IRequireRender.Render(IChartRenderContext icrc) {
			if (!Dirty) return;
			_trace.Verbose($"{Name} min:{Minimum} max:{Maximum} r:{Range}");
			// axis and tick marks
			AxisGeometry.Figures.Clear();
			var pf = PathHelper.Rectangle(Side == Side.Right ? 0 : icrc.Area.Width, Minimum, Side == Side.Right ? AxisLineThickness : icrc.Area.Width - AxisLineThickness, Maximum);
			AxisGeometry.Figures.Add(pf);
			if(!double.IsNaN(Minimum) && !double.IsNaN(Maximum)) {
				// recycle and layout
				DoTickLabels(icrc);
			}
			Dirty = false;
		}
		/// <summary>
		/// X-coordinates	"px"
		/// Y-coordinates	[0..1]
		/// </summary>
		/// <param name="icrc"></param>
		void IRequireTransforms.Transforms(IChartRenderContext icrc) {
			var scaley = icrc.Area.Height / Range;
			var matx = new Matrix(1, 0, 0, -scaley, icrc.Area.Left + AxisMargin * (Side == Side.Right ? 1 : -1), icrc.Area.Top + Maximum * scaley);
			AxisGeometry.Transform = new MatrixTransform() { Matrix = matx };
			_trace.Verbose($"transforms sy:{scaley:F3} matx:{matx} a:{icrc.Area} sa:{icrc.SeriesArea}");
			foreach (var tb in TickLabels) {
				var vx = (double)tb.Tag;
				tb.SetValue(Canvas.LeftProperty, icrc.Area.Left);
				var adj = tb.FontSize / 2;
				var top = icrc.Area.Bottom - (vx - Minimum) * scaley - adj;
				tb.SetValue(Canvas.TopProperty, top);
			}
		}
		#endregion
	}
	#endregion
}