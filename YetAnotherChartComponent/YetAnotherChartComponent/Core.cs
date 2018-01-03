﻿using System;
using System.Collections.Generic;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media;

namespace eScapeLLC.UWP.Charts {
	#region IChartAxis
	/// <summary>
	/// Allowed axis types.
	/// </summary>
	public enum AxisType {
		/// <summary>
		/// X-axis value.
		/// </summary>
		Category,
		/// <summary>
		/// Y-axis value.
		/// </summary>
		Value
	};
	/// <summary>
	/// Allowed axis orientations.
	/// </summary>
	public enum AxisOrientation {
		/// <summary>
		/// Horizontal orientation.
		/// </summary>
		Horizontal,
		/// <summary>
		/// Vertical orientation.
		/// </summary>
		Vertical
	};
	/// <summary>
	/// Axis visibility.
	/// </summary>
	public enum AxisVisibility {
		/// <summary>
		/// Visible and taking up layout space.
		/// </summary>
		Visible,
		/// <summary>
		/// Not visible and taking up layout space.
		/// </summary>
		Hidden,
		/// <summary>
		/// Not visible not taking up layout space.
		/// </summary>
		Collapsed
	};
	/// <summary>
	/// Features for axes.
	/// Axes must be present in the component list, to provide the infrastructure for scaling data series, even if they
	/// will not display.
	/// </summary>
	public interface IChartAxis {
		/// <summary>
		/// The axis type.
		/// </summary>
		AxisType Type { get; }
		/// <summary>
		/// The axis orientation.
		/// Typically Horizontal for Category and Vertical for Value.
		/// </summary>
		AxisOrientation Orientation { get; }
		/// <summary>
		/// The side of the data area this axis attaches to.
		/// Typically Bottom for Category and Right for Value.
		/// </summary>
		Side Side { get; }
		/// <summary>
		/// Minimum value or NaN.
		/// </summary>
		double Minimum { get; }
		/// <summary>
		/// Maximum value or NaN.
		/// </summary>
		double Maximum { get; }
		/// <summary>
		/// Range or NaN.
		/// </summary>
		double Range { get; }
		/// <summary>
		/// Reset the limits so axis can re-calculate.
		/// </summary>
		void ResetLimits();
		/// <summary>
		/// Do bookkeeping for updating limits/range.
		/// </summary>
		/// <param name="value">The value.</param>
		void UpdateLimits(double value);
		/// <summary>
		/// Map the value.
		/// </summary>
		/// <param name="value">Input (actual) value.</param>
		/// <returns>Axis-mapped value.</returns>
		double For(double value);
		/// <summary>
		/// Map the value with label.
		/// </summary>
		/// <param name="valueWithLabel">Value with its label.</param>
		/// <returns>Axis-mapped value.</returns>
		double For(Tuple<double, String> valueWithLabel);
	}
	#endregion
	#region IChartLayoutContext
	/// <summary>
	/// Side to claim space from.
	/// </summary>
	public enum Side {
		/// <summary>
		/// Top.
		/// </summary>
		Top,
		/// <summary>
		/// Right.
		/// </summary>
		Right,
		/// <summary>
		/// Bottom.
		/// </summary>
		Bottom,
		/// <summary>
		/// Left.
		/// </summary>
		Left,
		/// <summary>
		/// No fixed side, no space claimed.
		/// </summary>
		Float
	};
	/// <summary>
	/// Context interface for the layout process.
	/// </summary>
	public interface IChartLayoutContext {
		/// <summary>
		/// Overall dimensions.
		/// </summary>
		Size Dimensions { get; }
		/// <summary>
		/// Space remaining after claims.
		/// This rectangle is passed to all components via IChartRenderContext.SeriesArea.
		/// </summary>
		Rect RemainingRect { get; }
		/// <summary>
		/// Subtract space from RemainingRect and register that rectangle for given component.
		/// Returns the allocated rectangle.
		/// The claimed rectangle is passed back to this component via IChartRenderContext.Area.
		/// </summary>
		/// <param name="cc">Component key.</param>
		/// <param name="sd">Side to allocate from.</param>
		/// <param name="amt">Amount.  Refers to Height:Top/Bottom and Width:Left/Right.  Alternate dimension comes from the Dimensions property.</param>
		/// <returns>Allocated and registered rectangle.</returns>
		Rect ClaimSpace(ChartComponent cc, Side sd, double amt);
	}
	#endregion
	#region IChartRenderContext
	/// <summary>
	/// Feaatures for rendering.
	/// </summary>
	public interface IChartRenderContext {
		/// <summary>
		/// Current overall dimensions.
		/// </summary>
		Size Dimensions { get; }
		/// <summary>
		/// The area to render this component in.
		/// </summary>
		Rect Area { get; }
		/// <summary>
		/// The area where series are displayed.
		/// </summary>
		Rect SeriesArea { get; }
		/// <summary>
		/// The data context object.
		/// </summary>
		object DataContext { get; }
		/// <summary>
		/// Look up a component by name.
		/// </summary>
		/// <param name="name">Name.</param>
		/// <returns>Matching component or NULL.</returns>
		ChartComponent Find(String name);
		/// <summary>
		/// Add group of components.
		/// </summary>
		/// <param name="fes"></param>
		void Add(IEnumerable<FrameworkElement> fes);
		/// <summary>
		/// Remove group of components.
		/// </summary>
		/// <param name="fes"></param>
		void Remove(IEnumerable<FrameworkElement> fes);
	}
	#endregion
	#region IChartEnterLeaveContext
	/// <summary>
	/// Additional features for enter/leave.
	/// </summary>
	public interface IChartEnterLeaveContext : IChartRenderContext {
		/// <summary>
		/// Add content.
		/// </summary>
		/// <param name="fe">Element to add.</param>
		void Add(FrameworkElement fe);
		/// <summary>
		/// Remove content.
		/// </summary>
		/// <param name="fe">Element to remove.</param>
		void Remove(FrameworkElement fe);
	}
	#endregion
	#region IRequireLayout
	/// <summary>
	/// Require participation in layout pass.
	/// </summary>
	public interface IRequireLayout {
		/// <summary>
		/// Claim layout space before rendering begins.
		/// </summary>
		/// <param name="iclc">The context.</param>
		void Layout(IChartLayoutContext iclc);
	}
	#endregion
	#region IRequireEnterLeave
	/// <summary>
	/// Require component lifecycle.
	/// </summary>
	public interface IRequireEnterLeave {
		/// <summary>
		/// Component is entering the chart.
		/// Opportunity to add objects to the Visual Tree, then obtain/transfer bindings to those objects from the component's DPs.
		/// Framework makes an effort to defer this call until the VT is available.
		/// Example: components included directly in XAML via Chart.Components.
		/// </summary>
		/// <param name="icelc">The context.</param>
		void Enter(IChartEnterLeaveContext icelc);
		/// <summary>
		/// Component is leaving the chart.
		/// Opportunity to remove objects from Visual Tree etc. the dual of Enter().
		/// </summary>
		/// <param name="icelc">The context.</param>
		void Leave(IChartEnterLeaveContext icelc);
	}
	#endregion
	#region IRequireRender
	/// <summary>
	/// Require rendering pass.
	/// Use this interface if NOT using <see cref="IDataSourceRenderer"/>.
	/// </summary>
	public interface IRequireRender {
		/// <summary>
		/// Render the component.
		/// This is where data SHOULD be processed and Geometry etc. built.
		/// Non-geomerty drawing attributes MAY be configured here, but SHOULD have been arranged in ChartComponent.Enter.
		/// Geometry coordinates MUST be represented in layout-invariant coordinates!
		/// This means when the layout rectangle size changes, only the GeometryTransform is adjusted (in ChartComponent.Transforms); no data is re-calculated.
		/// </summary>
		/// <param name="icrc">The context.</param>
		void Render(IChartRenderContext icrc);
	}
	#endregion
	#region IRequireTransforms
	/// <summary>
	/// Require Transforms pass.
	/// </summary>
	public interface IRequireTransforms {
		/// <summary>
		/// Adjust transforms after layout and rendering are completed.
		/// </summary>
		/// <param name="icrc">The context.</param>
		void Transforms(IChartRenderContext icrc);
	}
	#endregion
	#region IProvideLegend
	/// <summary>
	/// Ability to participate in the legend items collection.
	/// </summary>
	public interface IProvideLegend {
		/// <summary>
		/// Create the legend for this item.
		/// </summary>
		/// <returns></returns>
		Legend Legend();
	}
	#endregion
	#region IProvideValueExtents
	/// <summary>
	/// Ability to provide value-axis extents.
	/// </summary>
	public interface IProvideValueExtents {
		/// <summary>
		/// The lowest value.
		/// If unset, MUST be double.NaN.
		/// </summary>
		double Minimum { get; }
		/// <summary>
		/// The highest value.
		/// If unset, MUST be double.NaN.
		/// </summary>
		double Maximum { get; }
	}
	#endregion
	#region IProvideCategoryExtents
	/// <summary>
	/// Ability to provide category-axis extents.
	/// </summary>
	public interface IProvideCategoryExtents {
		/// <summary>
		/// The lowest value.
		/// If unset, MUST be double.NaN.
		/// </summary>
		double CategoryMinimum { get; }
		/// <summary>
		/// The highest value.
		/// If unset, MUST be double.NaN.
		/// </summary>
		double CategoryMaximum { get; }
	}
	#endregion
	#region ChartComponent
	/// <summary>
	/// Refresh delegate.
	/// </summary>
	/// <param name="cc">Originating component.</param>
	public delegate void RefreshRequestEventHandler(ChartComponent cc);
	/// <summary>
	/// Base class of chart components.
	/// It is FrameworkElement primarily to participate in DataContext and Binding.
	/// </summary>
	public abstract class ChartComponent : FrameworkElement {
		#region ctor
		/// <summary>
		/// Default ctor.
		/// </summary>
		protected ChartComponent() { }
		#endregion
		#region events
		/// <summary>
		/// "External" interest in this component's updates.
		/// </summary>
		public event RefreshRequestEventHandler RefreshRequest;
		#endregion
		#region properties
		/// <summary>
		/// True: visuals require re-computing.
		/// </summary>
		public bool Dirty { get; set; }
		#endregion
		#region helpers
		/// <summary>
		/// Invoke the RefreshRequest event.
		/// </summary>
		protected void Refresh() { RefreshRequest?.Invoke(this); }
		/// <summary>
		/// Bind cc.Path to the given fe.DP.
		/// </summary>
		/// <param name="cc">Source chart component.</param>
		/// <param name="path">Component's (source) property path.</param>
		/// <param name="fe">Target framework element.</param>
		/// <param name="dp">FE's (target) DP.</param>
		protected static void BindTo(ChartComponent cc, String path, FrameworkElement fe, DependencyProperty dp) {
			Binding bx = new Binding() {
				Path = new PropertyPath(path),
				Source = cc,
				Mode = BindingMode.OneWay
			};
			fe.SetBinding(dp, bx);
		}
		#endregion
	}
	#endregion
	#region TreeHelper
	/// <summary>
	/// Static Helpers for visual tree.
	/// </summary>
	public static class TreeHelper {
		/// <summary>
		/// Finds object in control's template by its name.
		/// </summary>
		/// <param name="name">Object's name.</param>
		/// <param name="templatedParent">Templated parent.</param>
		/// <returns>!NULL: found object; NULL: otherwise.</returns>
		public static object TemplateFindName(string name, FrameworkElement templatedParent) {
			for (int ix = 0; ix < VisualTreeHelper.GetChildrenCount(templatedParent); ix++) {
				var child = VisualTreeHelper.GetChild(templatedParent, ix);
				if (child is FrameworkElement) {
					if ((child as FrameworkElement).Name == name) {
						return child;
					} else {
						var subChild = TemplateFindName(name, child as FrameworkElement);
						if (subChild is FrameworkElement && (subChild as FrameworkElement).Name == name) {
							return subChild;
						}
					}
				}
			}
			return null;
		}
	}
	#endregion
	#region BindingEvaluator
	/// <summary>
	/// Utility class to facilitate runtime binding evaluation.
	/// </summary>
	public class BindingEvaluator : FrameworkElement {
		private readonly PropertyPath _pp;
		/// <summary>
		/// Created binding evaluator and set path to the property which's value should be evaluated.
		/// </summary>
		/// <param name="propertyPath">Path to the property.</param>
		public BindingEvaluator(string propertyPath) {
			_pp = new PropertyPath(propertyPath);
		}
		/// <summary>
		/// Dependency property used to evaluate values.
		/// </summary>
		public static readonly DependencyProperty EvaluatorProperty = DependencyProperty.Register("Evaluator", typeof(object), typeof(BindingEvaluator), null);
		/// <summary>
		/// Returns value of property on provided object.
		/// </summary>
		/// <param name="source">Object to evaluate property for.</param>
		/// <returns>Value of the property.</returns>
		public object For(object source) {
			// ClearValue() is not needed
			//ClearValue(EvaluatorProperty);
			var binding = new Binding {
				Path = _pp,
				Mode = BindingMode.OneTime,
				Source = source
			};
			SetBinding(EvaluatorProperty, binding);
			return GetValue(EvaluatorProperty);
		}
	}
	#endregion
	#region PathHelper
	/// <summary>
	/// Static methods for creating PathFigures.
	/// </summary>
	public static class PathHelper {
		/// <summary>
		/// Build Closed PathFigure for given rectangle.
		/// Does not check for coordinates' min/max because the Geometry Transform is not known here.
		/// </summary>
		/// <param name="left"></param>
		/// <param name="top"></param>
		/// <param name="right"></param>
		/// <param name="bottom"></param>
		/// <returns></returns>
		public static PathFigure Rectangle(double left, double top, double right, double bottom) {
			var pf = new PathFigure { StartPoint = new Windows.Foundation.Point(left, top) };
			var ls = new LineSegment() { Point = new Windows.Foundation.Point(left, bottom) };
			pf.Segments.Add(ls);
			ls = new LineSegment() { Point = new Windows.Foundation.Point(right, bottom) };
			pf.Segments.Add(ls);
			ls = new LineSegment() { Point = new Windows.Foundation.Point(right, top) };
			pf.Segments.Add(ls);
			pf.IsClosed = true;
			return pf;
		}
		/// <summary>
		/// Build Open PathFigure for given line segment.
		/// </summary>
		/// <param name="startx"></param>
		/// <param name="starty"></param>
		/// <param name="endx"></param>
		/// <param name="endy"></param>
		/// <returns></returns>
		public static PathFigure Line(double startx, double starty, double endx, double endy) {
			var pf = new PathFigure { StartPoint = new Windows.Foundation.Point(startx, starty) };
			var ls = new LineSegment() { Point = new Windows.Foundation.Point(startx, endy) };
			pf.Segments.Add(ls);
			return pf;
		}
	}
	#endregion
	#region converters
	/// <summary>
	/// Converter for bool to Visibility.
	/// </summary>
	public class BoolToVisibilityConverter : IValueConverter {
		/// <summary>
		/// convert.
		/// </summary>
		/// <param name="value"></param>
		/// <param name="targetType"></param>
		/// <param name="parameter"></param>
		/// <param name="language"></param>
		/// <returns></returns>
		public object Convert(object value, Type targetType, object parameter, string language) {
			var isChecked = (bool)value;
			return isChecked ? Visibility.Visible : Visibility.Collapsed;
		}
		/// <summary>
		/// Unconvert.
		/// </summary>
		/// <param name="value"></param>
		/// <param name="targetType"></param>
		/// <param name="parameter"></param>
		/// <param name="language"></param>
		/// <returns></returns>
		public object ConvertBack(object value, Type targetType, object parameter, string language) {
			throw new NotImplementedException();
		}
	}
	#endregion
	#region Recycler
	/// <summary>
	/// Recycles an input list of instances, then provides new instances.
	/// Does the bookkeeping to track unused and newly-provided instances.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public class Recycler<T> {
		#region data
		readonly List<T> _unused = new List<T>();
		readonly List<T> _created = new List<T>();
		readonly IEnumerable<T> _source;
		readonly Func<T> _factory;
		#endregion
		#region properties
		/// <summary>
		/// Original items that were not used up by iterating.
		/// </summary>
		public IEnumerable<T> Unused { get { return _unused; } }
		/// <summary>
		/// Excess items that were created after original items were used up.
		/// </summary>
		public IEnumerable<T> Created { get { return _created; } }
		#endregion
		#region ctor
		/// <summary>
		/// Ctor.
		/// </summary>
		/// <param name="source">Initial list to reuse; MAY be empty.</param>
		/// <param name="factory">Used to create new instances when SOURCE runs out.</param>
		public Recycler(IEnumerable<T> source, Func<T> factory) {
#pragma warning disable IDE0016 // Use 'throw' expression
			if (source == null) throw new ArgumentNullException(nameof(source));
			if (factory == null) throw new ArgumentNullException(nameof(factory));
#pragma warning restore IDE0016 // Use 'throw' expression
			_unused.AddRange(source);
			_source = source;
			_factory = factory;
		}
		#endregion
		#region public
		/// <summary>
		/// First exhaust the original source, then start creating new instances until no longer iterating.
		/// Do the bookkeeping for used and created lists.
		/// DO NOT use this to control looping!
		/// </summary>
		/// <returns>Another instance.  MAY be newly created.</returns>
		public IEnumerable<T> Items() {
			foreach(var tx in _source) {
				_unused.Remove(tx);
				yield return tx;
			}
			while(true) {
				var tx = _factory();
				_created.Add(tx);
				yield return tx;
			}
		}
		#endregion
	}
	#endregion
	#region Legend
	/// <summary>
	/// Base VM for the chart legend.
	/// </summary>
	public class Legend {
		/// <summary>
		/// The color swatch to display.
		/// </summary>
		public Brush Fill { get; set; }
		/// <summary>
		/// The border for the swatch.
		/// </summary>
		public Brush Stroke { get; set; }
		/// <summary>
		/// The title.
		/// </summary>
		public String Title { get; set; }
	}
	#endregion
}