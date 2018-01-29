﻿using eScape.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace eScapeLLC.UWP.Charts {
	/// <summary>
	/// Component that creates series value labels.
	/// </summary>
	public class SeriesValueLabels : ChartComponent, IDataSourceRenderer, IRequireChartTheme, IRequireEnterLeave, IRequireTransforms {
		static LogTools.Flag _trace = LogTools.Add("SeriesValueLabels", LogTools.Level.Error);
		#region SeriesItemState
		/// <summary>
		/// Shorthand for item state.
		/// </summary>
		protected class SeriesItemState : ItemState<TextBlock> { }
		#endregion
		#region properties
		/// <summary>
		/// Holder for IRequireChartTheme interface.
		/// </summary>
		public IChartTheme Theme { get; set; }
		/// <summary>
		/// The name of the data source in the DataSources collection.
		/// </summary>
		public String DataSourceName { get { return (String)GetValue(DataSourceNameProperty); } set { SetValue(DataSourceNameProperty, value); } }
		/// <summary>
		/// Component name of value axis.
		/// Referenced component MUST implement IChartAxis.
		/// </summary>
		public String ValueAxisName { get; set; }
		/// <summary>
		/// Component name of category axis.
		/// Referenced component MUST implement IChartAxis.
		/// </summary>
		public String CategoryAxisName { get; set; }
		/// <summary>
		/// Binding path to the category axis value.
		/// MAY be NULL, in which case the data-index is used instead.
		/// </summary>
		public String CategoryPath { get { return (String)GetValue(CategoryPathProperty); } set { SetValue(CategoryPathProperty, value); } }
		/// <summary>
		/// Binding path to the value axis value.
		/// </summary>
		public String ValuePath { get { return (String)GetValue(ValuePathProperty); } set { SetValue(ValuePathProperty, value); } }
		/// <summary>
		/// The style to apply to labels.
		/// </summary>
		public Style LabelStyle { get { return (Style)GetValue(LabelStyleProperty); } set { SetValue(LabelStyleProperty, value); } }
		/// <summary>
		/// Alternate format string for labels.
		/// </summary>
		public String LabelFormatString { get; set; }
		/// <summary>
		/// Offset in Category axis offset in [0..1].
		/// Use with ColumnSeries to get the "points" to align with the column(s) layout in their cells.
		/// </summary>
		public double CategoryAxisOffset { get; set; }
		/// <summary>
		/// LabelOffset is translation from the "center" of the TextBlock.
		/// Units are Half-dimension based on TextBlock size.
		/// Default value is (0,0).
		/// </summary>
		public Point LabelOffset { get; set; } = new Point(0, 0);
		/// <summary>
		/// Dereferenced value axis.
		/// </summary>
		protected IChartAxis ValueAxis { get; set; }
		/// <summary>
		/// Dereferenced category axis.
		/// </summary>
		protected IChartAxis CategoryAxis { get; set; }
		/// <summary>
		/// The layer for components.
		/// </summary>
		protected IChartLayer Layer { get; set; }
		/// <summary>
		/// Current item state.
		/// </summary>
		protected List<SeriesItemState> ItemState { get; set; }
		#endregion
		#region DPs
		/// <summary>
		/// Identifies <see cref="CategoryPath"/> dependency property.
		/// </summary>
		public static readonly DependencyProperty CategoryPathProperty = DependencyProperty.Register(
			nameof(CategoryPath), typeof(string), typeof(SeriesValueLabels), new PropertyMetadata(null, new PropertyChangedCallback(PropertyChanged_ValueDirty))
		);
		/// <summary>
		/// Identifies <see cref="DataSourceName"/> dependency property.
		/// </summary>
		public static readonly DependencyProperty DataSourceNameProperty = DependencyProperty.Register(
			nameof(DataSourceName), typeof(string), typeof(SeriesValueLabels), new PropertyMetadata(null, new PropertyChangedCallback(PropertyChanged_ValueDirty))
		);
		/// <summary>
		/// Identifies <see cref="LabelStyle"/> dependency property.
		/// </summary>
		public static readonly DependencyProperty LabelStyleProperty = DependencyProperty.Register(
			nameof(LabelStyle), typeof(Style), typeof(SeriesValueLabels), new PropertyMetadata(null)
		);
		/// <summary>
		/// Identifies <see cref="ValuePath"/> dependency property.
		/// </summary>
		public static readonly DependencyProperty ValuePathProperty = DependencyProperty.Register(
			nameof(ValuePath), typeof(string), typeof(SeriesValueLabels), new PropertyMetadata(null, new PropertyChangedCallback(PropertyChanged_ValueDirty))
		);
		#endregion
		#region ctor
		/// <summary>
		/// Ctor.
		/// </summary>
		public SeriesValueLabels() {
			ItemState = new List<SeriesItemState>();
		}
		#endregion
		#region helpers
		/// <summary>
		/// Resolve axis references.
		/// </summary>
		/// <param name="icrc">The context.</param>
		protected void EnsureAxes(IChartComponentContext icrc) {
			if (ValueAxis == null && !String.IsNullOrEmpty(ValueAxisName)) {
				ValueAxis = icrc.Find(ValueAxisName) as IChartAxis;
			} else {
				if (icrc is IChartErrorInfo icei) {
					icei.Report(new ChartValidationResult(NameOrType(), $"Value axis '{ValueAxisName}' was not found", new[] { nameof(ValueAxis), nameof(ValueAxisName) }));
				}
			}
			if (CategoryAxis == null && !String.IsNullOrEmpty(CategoryAxisName)) {
				CategoryAxis = icrc.Find(CategoryAxisName) as IChartAxis;
			} else {
				if (icrc is IChartErrorInfo icei) {
					icei.Report(new ChartValidationResult(NameOrType(), $"Category axis '{CategoryAxisName}' was not found", new[] { nameof(CategoryAxis), nameof(CategoryAxisName) }));
				}
			}
		}
		#endregion
		#region IRequireEnterLeave
		void IRequireEnterLeave.Enter(IChartEnterLeaveContext icelc) {
			EnsureAxes(icelc as IChartComponentContext);
			Layer = icelc.CreateLayer();
			AssignFromRef(icelc as IChartErrorInfo, NameOrType(), nameof(LabelStyle), nameof(Theme.LabelAxisTop),
				LabelStyle == null && Theme != null,
				Theme.LabelAxisTop != null,
				() => LabelStyle = Theme.LabelAxisTop
			);
			_trace.Verbose($"{Name} enter v:{ValueAxisName} {ValueAxis} c:{CategoryAxisName} {CategoryAxis} d:{DataSourceName}");
		}
		void IRequireEnterLeave.Leave(IChartEnterLeaveContext icelc) {
			_trace.Verbose($"{Name} leave");
			ValueAxis = null;
			CategoryAxis = null;
			icelc.DeleteLayer(Layer);
			Layer = null;
		}
		#endregion
		#region IRequireTransforms
		/// <summary>
		/// Adjust transforms for the current element state.
		/// </summary>
		/// <param name="icrc"></param>
		void IRequireTransforms.Transforms(IChartRenderContext icrc) {
			if (CategoryAxis == null || ValueAxis == null) return;
			if (ItemState.Count == 0) return;
			_trace.Verbose($"{Name} transforms a:{icrc.Area} rx:{CategoryAxis.Range} ry:{ValueAxis.Range}");
			var matx = MatrixSupport.TransformFor(icrc.Area, CategoryAxis, ValueAxis);
			foreach (var state in ItemState) {
				var dcc = matx.Transform(new Point(state.XValue, state.YValue));
				// get half-dimensions of the TextBlock
				// IST elements must have had measure-pass before we get to here!
				var hw = state.Element.ActualWidth / 2;
				var hh = state.Element.ActualHeight / 2;
				state.Element.SetValue(Canvas.LeftProperty, dcc.X - hw + state.Element.ActualWidth*LabelOffset.X);
				state.Element.SetValue(Canvas.TopProperty, dcc.Y - hh + state.Element.ActualHeight*LabelOffset.Y);
#if false
				if (ClipToDataRegion) {
					// TODO this does not work "correctly" the TB gets clipped no matter what
					// this is because the clip coordinate system is for "inside" the text block (gotta verify this)
					// must find intersection of the TB bounds and the icrc.SeriesArea, and make that the clip.
					//state.Element.Clip = new RectangleGeometry() { Rect = icrc.SeriesArea };
				}
#endif
				_trace.Verbose($"{Name} matx:{matx} pt:({state.XValue},{state.YValue}) dcc:{dcc} tbsz:{state.Element.ActualWidth},{state.Element.ActualHeight}");
			}
		}
		#endregion
		#region IDataSourceRenderer
		/// <summary>
		/// Element factory for recycler.
		/// </summary>
		/// <returns></returns>
		TextBlock CreateElement() {
			var tb = new TextBlock();
			if (LabelStyle != null) {
				tb.Style = LabelStyle;
			}
			return tb;
		}
		object IDataSourceRenderer.Preamble(IChartRenderContext icrc) {
			if (ValueAxis == null || CategoryAxis == null) return null;
			if (String.IsNullOrEmpty(ValuePath)) return null;
			var by = new BindingEvaluator(ValuePath);
			// TODO report the binding error
			if (by == null) return null;
			//ResetLimits();
			var elements = ItemState.Select(ms => ms.Element);
			var recycler = new Recycler<TextBlock>(elements, CreateElement);
			return new RenderState_ValueAndLabel<SeriesItemState, TextBlock>(new List<SeriesItemState>(), recycler,
				!String.IsNullOrEmpty(CategoryPath) ? new BindingEvaluator(CategoryPath) : null,
				null, by
			);
		}
		void IDataSourceRenderer.Render(object state, int index, object item) {
			var st = state as RenderState_ValueAndLabel<SeriesItemState, TextBlock>;
			var valuey = DataSeries.CoerceValue(item, st.by);
			var valuex = st.bx != null ? (double)st.bx.For(item) : index;
			valuex += CategoryAxisOffset;
			st.ix = index;
			//UpdateLimits(valuex, valuey);
			// short-circuit if it's NaN
			if (double.IsNaN(valuey)) {
				return;
			}
			var mappedy = ValueAxis.For(valuey);
			var mappedx = CategoryAxis.For(valuex);
			// finish up
			var tb = st.NextElement();
			if (tb == null) return;
			tb.Text = valuey.ToString(String.IsNullOrEmpty(LabelFormatString) ? "G" : LabelFormatString);
			var sis = new SeriesItemState() { Element = tb, YValue = mappedy, XValue = mappedx, Index = index };
			st.itemstate.Add(sis);
		}
		void IDataSourceRenderer.RenderComplete(object state) {
			var st = state as RenderState_ValueAndLabel<SeriesItemState, TextBlock>;
			if (st.bx == null) {
				// needs one extra "cell"
				//UpdateLimits(st.ix + 1, double.NaN);
			}
		}
		void IDataSourceRenderer.Postamble(object state) {
			var st = state as RenderState_ValueAndLabel<SeriesItemState, TextBlock>;
			ItemState = st.itemstate;
			Layer.Remove(st.recycler.Unused);
			Layer.Add(st.recycler.Created);
			var sz = new Size(1024, 1024);
			foreach(var xx in st.recycler.Created) {
				if (xx.DesiredSize.Width == 0 || xx.DesiredSize.Height == 0) {
					// force it to measure; needed for Transforms
					xx.Measure(sz);
				}
			}
			Dirty = false;
		}
		#endregion
	}
}
