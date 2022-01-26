#region copyright

/* * * * * * * * * * * * * * * * * * * * * * * * * */
/* Carl Zeiss IMT (IZfM Dresden)                   */
/* Softwaresystem PiWeb                            */
/* (c) Carl Zeiss 2016                             */
/* * * * * * * * * * * * * * * * * * * * * * * * * */

#endregion

namespace Zeiss.PiWeb.Volume.UI.Extensions
{
	#region usings

	using System;
	using System.Collections;
	using System.Globalization;
	using System.Linq;
	using System.Windows;
	using System.Windows.Data;
	using System.Windows.Markup;
	using System.Windows.Media;

	#endregion

	/// <summary>
	/// Custom markup extension for handling simple binding conditions and conversion to reduce use of converters.
	///
	/// Examples:
	/// {IfBinding MyValue, Is=LessThan, Value=10, Then={Resx TooLow}, Else={Resx WellDone}}
	/// {IfBinding ElementName=ToggleButton, Path=IsChecked}
	/// {IfBinding SelectedValue, Is=EqualTo, Value={x:Static ns:EnumType.Value2}, Then={x:Static Colors.Red}, Else={x:Static Colors.Lime}}
	///
	/// </summary>
	[MarkupExtensionReturnType( typeof( object ) )]
	public class IfBindingExtension : Binding
	{
		#region constructors

		/// <summary>
		/// Creates a new conditional binding.
		/// </summary>
		/// <param name="path">Binding path.</param>
		public IfBindingExtension( string path ) : base( path )
		{
			base.Converter = ConditionConverter.Default;
			base.ConverterParameter = this;
			ConverterCulture = CultureInfo.CurrentCulture;
		}

		/// <summary>
		/// Creates a new conditional binding.
		/// </summary>
		public IfBindingExtension()
		{
			base.Converter = ConditionConverter.Default;
			base.ConverterParameter = this;
			ConverterCulture = CultureInfo.CurrentCulture;
		}

		#endregion

		#region properties

		/// <summary>
		/// Gets or sets the kind of condition.
		/// </summary>
		public ConditionType Is { get; set; } = ConditionType.True;

		/// <summary>
		/// Gets or sets the value that is used for compare conditions.
		/// </summary>
		public object Value { get; set; }

		/// <summary>
		/// Gets or sets a value that is used if the given condition is true.
		/// </summary>
		public object Then { get; set; } = true;

		/// <summary>
		/// Gets or sets a value that is used if the given condition is false.
		/// </summary>
		public object Else { get; set; } = false;

		/// <summary>
		/// Gets or sets an optional Converter.
		/// </summary>
		public new IValueConverter Converter { get; set; }

		/// <summary>
		/// Gets or sets an optional ConverterParameter.
		/// </summary>
		public new object ConverterParameter { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether to use <see cref="Visibility.Hidden"/> as false value for visibilites.
		/// </summary>
		public bool HideIfNotVisible { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether a ConvertBack is allowed.
		/// <remarks>
		/// This property is useful when used together with e.g. a ToggleButton.IsChecked to determine what should happen if it should be unchecked.
		/// </remarks>
		/// </summary>
		public bool DisableConvertBack { get; set; }

		#endregion

		public static object AutoConvertValue( object value, Type targetType, bool hideIfNotVisible = false )
		{
			if( value != null && targetType != typeof( string ) && !targetType.IsInstanceOfType( value ) )
			{
				if( targetType == typeof( Visibility ) )
				{
					if( value is bool boolValue )
						return boolValue ? Visibility.Visible : hideIfNotVisible ? Visibility.Hidden : Visibility.Collapsed;
				}
				else if( targetType.IsAssignableFrom( typeof( Brush ) ) )
				{
					if( value is Color colorValue )
						return new SolidColorBrush( colorValue );
				}
				else if( typeof( IConvertible ).IsAssignableFrom( targetType ) )
				{
					try
					{
						return Convert.ChangeType( value, targetType, CultureInfo.InvariantCulture );
					}
					catch( Exception )
					{
						// ignore
					}
				}
			}
			else if( targetType == typeof( string ) )
			{
				return value?.ToString() ?? "null";
			}

			return value;
		}

		#region class ConditionConverter

		private class ConditionConverter : IValueConverter
		{
			#region members

			public static readonly ConditionConverter Default = new ConditionConverter();

			#endregion

			#region constructors

			private ConditionConverter()
			{ }

			#endregion

			#region methods

			private static int ToInt( object value, int defaultValue = 0 )
			{
				if( value is int i )
					return i;

				if( value is long l )
					return (int) l;

				if( value is float f )
					return (int) f;

				if( value is double d )
					return (int) d;

				if( !int.TryParse( value as string, NumberStyles.Integer, CultureInfo.InvariantCulture, out var number ) )
					return number;

				return defaultValue;
			}

			#endregion

			#region interface IValueConverter

			public object Convert( object value, Type targetType, object parameter, CultureInfo culture )
			{
				var ifBinding = parameter as IfBindingExtension ?? throw new ArgumentNullException( nameof( parameter ), @$"Expect parameter to be instance of ""{nameof( IfBindingExtension )}""" );

				if( ifBinding.Converter != null )
					value = ifBinding.Converter.Convert( value, targetType, ifBinding.ConverterParameter, culture );

				return AutoConvertValue( Evaluate( value, ifBinding ), targetType, ifBinding.HideIfNotVisible );
			}

			private object Evaluate( object value, IfBindingExtension ifBinding )
			{
				if( ifBinding.Is == ConditionType.Convertable )
				{
					return value;
				}

				return EvaluateCondition( value, ifBinding ) ? ifBinding.Then : ifBinding.Else;
			}

			private static bool EvaluateCondition( object value, IfBindingExtension ifBinding )
			{
				return ifBinding.Is switch
				{
					ConditionType.True             => (bool?)value == true,
					ConditionType.False            => (bool?)value == false,
					ConditionType.Null             => value == null,
					ConditionType.NullOrTrue       => (bool?)value ?? true,
					ConditionType.NullOrFalse      => !( (bool?)value ?? false ),
					ConditionType.NotNull          => value != null,
					ConditionType.NullOrEmpty      => value == null || !( ( value as IEnumerable )?.Cast<object>().Any() ?? true ),
					ConditionType.NotEmpty         => ( value as IEnumerable )?.Cast<object>().Any() ?? false,
					ConditionType.Empty            => !( value as IEnumerable )?.Cast<object>().Any() ?? false,
					ConditionType.EqualTo          => Equals( value, ifBinding.Value ),
					ConditionType.NotEqualTo       => !Equals( value, ifBinding.Value ),
					ConditionType.LessThan         => ToInt( value ) < ToInt( ifBinding.Value, 1 ),
					ConditionType.GreaterThan      => ToInt( value ) > ToInt( ifBinding.Value ),
					ConditionType.LessOrEqualTo    => Equals( value, ifBinding.Value ) || ToInt( value ) < ToInt( ifBinding.Value, 1 ),
					ConditionType.GreaterOrEqualTo => Equals( value, ifBinding.Value ) || ToInt( value ) > ToInt( ifBinding.Value ),
					ConditionType.TypeOf           => ( ifBinding.Value as Type )?.IsInstanceOfType( value ) ?? false,
					ConditionType.NotTypeOf        => !( ( ifBinding.Value as Type )?.IsInstanceOfType( value ) ?? false ),
					ConditionType.Zero             => Equals( ToInt( value ), 0 ),
					ConditionType.NonZero          => !Equals( ToInt( value ), 0 ),
					ConditionType.One              => Equals( ToInt( value ), 1 ),
					ConditionType.Positive         => ToInt( value ) >= 0,
					ConditionType.Negative         => ToInt( value ) < 0,
					ConditionType.EnumFlagSet      => ( (Enum)value ).HasFlag( (Enum)ifBinding.Value ),
					ConditionType.EnumFlagNotSet   => !( (Enum)value ).HasFlag( (Enum)ifBinding.Value ),
					ConditionType.Containing       => ( value as IEnumerable )?.Cast<object>().Contains( ifBinding.Value ) ?? false,
					ConditionType.NotContaining    => !( ( value as IEnumerable )?.Cast<object>().Contains( ifBinding.Value ) ?? true ),
					ConditionType.ContainedIn      => ( ifBinding.Value as IEnumerable )?.Cast<object>().Contains( value ) ?? false,
					ConditionType.NotContainedIn   => !( ( ifBinding.Value as IEnumerable )?.Cast<object>().Contains( value ) ?? true ),
					_                              => throw new InvalidOperationException( $"Unknown condition type: {ifBinding.Is}" )
				};
			}

			public object ConvertBack( object value, Type targetType, object parameter, CultureInfo culture )
			{
				var ifBinding = parameter as IfBindingExtension ?? throw new ArgumentNullException( nameof( parameter ), @$"Expect parameter to be instance of ""{nameof( IfBindingExtension )}""" );

				if( ifBinding.DisableConvertBack )
					value = ifBinding.Value;
				else
				{
					value = ifBinding.Is switch
					{
						ConditionType.EqualTo => Equals( value, true ) ? ifBinding.Value : DoNothing,
						ConditionType.True    => Equals( value, true ),
						ConditionType.False   => Equals( value, false ),
						_                     => DependencyProperty.UnsetValue
					};
				}

				if( ifBinding.Converter != null )
					value = ifBinding.Converter.ConvertBack( value, targetType, ifBinding.ConverterParameter, culture );

				return value;
			}

			#endregion
		}

		#endregion

		#region ConditionType enum

		/// <summary>
		/// Enum of available conditions.
		/// </summary>
		public enum ConditionType
		{
			/// <summary>
			/// Checks for boolean true.
			/// </summary>
			True,

			/// <summary>
			/// Checks for boolean false.
			/// </summary>
			False,

			/// <summary>
			/// Checks for a null object.
			/// </summary>
			Null,

			/// <summary>
			/// Checks nullable for null or boolean true.
			/// </summary>
			NullOrTrue,

			/// <summary>
			/// Checks nullable for null or boolean false.
			/// </summary>
			NullOrFalse,

			/// <summary>
			/// Checks for object to be initialized.
			/// </summary>
			NotNull,

			/// <summary>
			/// Checks for null objects or empty enumerations (includes string).
			/// </summary>
			NullOrEmpty,

			/// <summary>
			/// Checks for initialized enumerations (includes string) that have at least one item.
			/// </summary>
			NotEmpty,

			/// <summary>
			/// Checks for initialized enumerations (includes string) that have are empty.
			/// </summary>
			Empty,

			/// <summary>
			/// Checks for equality.
			/// </summary>
			EqualTo,

			/// <summary>
			/// Checks for non equality.
			/// </summary>
			NotEqualTo,

			/// <summary>
			/// Checks if it is less.
			/// </summary>
			LessThan,

			/// <summary>
			/// Checks if it is greater.
			/// </summary>
			GreaterThan,

			/// <summary>
			/// Checks if it is less or equal.
			/// </summary>
			LessOrEqualTo,

			/// <summary>
			/// Checks if it is greater or equal.
			/// </summary>
			GreaterOrEqualTo,

			/// <summary>
			/// Checks whether an enum flag is set.
			/// </summary>
			EnumFlagSet,

			/// <summary>
			/// Checks whether an enum flag is not set.
			/// </summary>
			EnumFlagNotSet,

			/// <summary>
			/// Checks if the type matches.
			/// </summary>
			TypeOf,

			/// <summary>
			/// Checks if the type not matches.
			/// </summary>
			NotTypeOf,

			/// <summary>
			/// Checks if it is equal to zero.
			/// </summary>
			Zero,

			/// <summary>
			/// Checks if it is non equal to zero.
			/// </summary>
			NonZero,

			/// <summary>
			/// Checks if it is equal to one.
			/// </summary>
			One,

			/// <summary>
			/// Checks if it is a positive value.
			/// </summary>
			Positive,

			/// <summary>
			/// Checks if it is a negative value.
			/// </summary>
			Negative,

			/// <summary>
			/// Checks if it contains a specific value.
			/// </summary>
			Containing,

			/// <summary>
			/// Checks if it not contains a specific value.
			/// </summary>
			NotContaining,

			/// <summary>
			/// Checks if it is contained within an enumeration.
			/// </summary>
			ContainedIn,

			/// <summary>
			/// Checks if it is not contained within an enumeration.
			/// </summary>
			NotContainedIn,

			/// <summary>
			/// Simply passes the binded value.
			/// </summary>
			Convertable,
		}

		#endregion
	}
}