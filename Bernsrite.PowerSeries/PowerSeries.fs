/// Inspired by "Power Series, Power Serious" by M. Douglas McIlroy
/// http://citeseerx.ist.psu.edu/viewdoc/download?doi=10.1.1.333.3156&rep=rep1&type=pdf
namespace Bernsrite.PowerSeries

open System
open Microsoft.FSharp.Core.LanguagePrimitives
open MathNet.Numerics

module List =

    /// Aliases for list constructors, so we can still access them after overriding their
    /// usual names.
    let (|Cons|Nil|) = function
        | [] -> Nil
        | head :: tail -> Cons(head, tail)

/// A power series: a0 + a1*x + a2*x^2 + a3*x^3 + ...
type PowerSeries<'T
        when ^T : (static member Zero : ^T)
        and ^T : (static member One : ^T)
        and ^T : (static member (+) : ^T * ^T -> ^T)
        and ^T : (static member (*) : ^T * ^T -> ^T)
        and ^T : (static member (/) : ^T * ^T -> ^T)
        and ^T : (static member (~-) : ^T -> ^T)
        and ^T : equality> =
    | (::) of ('T * Lazy<PowerSeries<'T>>)

module internal Internal =

    /// Power series for 0.
    let inline zero<'T
            when ^T : (static member Zero : ^T)
            and ^T : (static member One : ^T)
            and ^T : (static member (+) : ^T * ^T -> ^T)
            and ^T : (static member (*) : ^T * ^T -> ^T)
            and ^T : (static member (/) : ^T * ^T -> ^T)
            and ^T : (static member (~-) : ^T -> ^T)
            and ^T : equality> =
        let rec value =  GenericZero<'T> :: lazy value
        value

    /// Power series for a constant.
    let inline constant<'T
                when ^T : (static member Zero : ^T)
                and ^T : (static member One : ^T)
                and ^T : (static member (+) : ^T * ^T -> ^T)
                and ^T : (static member (*) : ^T * ^T -> ^T)
                and ^T : (static member (/) : ^T * ^T -> ^T)
                and ^T : (static member (~-) : ^T -> ^T)
                and ^T : equality> (c : 'T) =
        c :: lazy zero

    /// Power series for 1.
    let inline one<'T
            when ^T : (static member Zero : ^T)
            and ^T : (static member One : ^T)
            and ^T : (static member (+) : ^T * ^T -> ^T)
            and ^T : (static member (*) : ^T * ^T -> ^T)
            and ^T : (static member (/) : ^T * ^T -> ^T)
            and ^T : (static member (~-) : ^T -> ^T)
            and ^T : equality> =
        constant GenericOne<'T>

    /// 0 + 1*x
    let inline x<'T
            when ^T : (static member Zero : ^T)
            and ^T : (static member One : ^T)
            and ^T : (static member (+) : ^T * ^T -> ^T)
            and ^T : (static member (*) : ^T * ^T -> ^T)
            and ^T : (static member (/) : ^T * ^T -> ^T)
            and ^T : (static member (~-) : ^T -> ^T)
            and ^T : equality> =
        GenericZero<'T> :: lazy (GenericOne<'T> :: lazy zero)

    /// Constructs a power series from the given coeffecients.
    let inline ofList<'T
            when ^T : (static member Zero : ^T)
            and ^T : (static member One : ^T)
            and ^T : (static member (+) : ^T * ^T -> ^T)
            and ^T : (static member (*) : ^T * ^T -> ^T)
            and ^T : (static member (/) : ^T * ^T -> ^T)
            and ^T : (static member (~-) : ^T -> ^T)
            and ^T : equality> (ns : List<'T>) =
        let rec loop = function
            | List.Nil -> zero
            | List.Cons (head, tail) -> head :: lazy (loop tail)
        ns |> loop

    /// Negates the given power series.
    let inline negate series =
        let rec loop = function
            | f :: fs -> -f :: lazy (loop fs.Value)
        series |> loop

    /// Scales the given power series by a constant.
    let inline scale (c : 'T) series =
        let rec loop (f :: fs) =
            (c * f) :: lazy (fs.Value |> loop)
        series |> loop

    /// Adds the given power series.
    let inline add seriesF seriesG =
        let rec loop (f : 'T :: fs) (g : 'T :: gs) =
            (f + g) :: lazy (loop fs.Value gs.Value)
        loop seriesF seriesG

    /// Subtracts the given power series.
    let inline sub seriesF seriesG =
        add seriesF (negate seriesG)

    /// Multiplies the given power series.
    let inline mult seriesF seriesG =
        let rec loop (f : 'T :: fs) (g : 'T :: gs) =
            (f * g) :: lazy (add (scale f gs.Value) (loop fs.Value (g :: gs)))
        loop seriesF seriesG

    /// Divides the given power series.
    let inline div seriesF seriesG =
        let rec loop (f : 'T :: fs) (g : 'T :: gs) =
            if f = GenericZero<'T> && g = GenericZero<'T> then
                loop fs.Value gs.Value
            else
                let q = f / g
                q :: lazy (loop (sub fs.Value (scale q gs.Value)) (g :: gs))
        loop seriesF seriesG

    /// Raises the given power series to a power.
    let inline pow n series =
        let rec loop n series =
            match n with
                | 0 -> one
                | n when n > 0 ->
                    series
                        |> loop (n - 1)
                        |> mult series
                | _ -> failwith "Negative exponents not supported"
        series |> loop n

/// Power series extensions.
type PowerSeries<'T
        when ^T : (static member Zero : ^T)
        and ^T : (static member One : ^T)
        and ^T : (static member (+) : ^T * ^T -> ^T)
        and ^T : (static member (*) : ^T * ^T -> ^T)
        and ^T : (static member (/) : ^T * ^T -> ^T)
        and ^T : (static member (~-) : ^T -> ^T)
        and ^T : equality> with

    /// Power series for 0.
    static member inline Zero =
        Internal.zero<'T>

    /// Power series for 1.
    static member inline One =
        Internal.one<'T>

    /// 0 + 1*x
    static member inline X =
        Internal.x<'T>

    /// Negates the given power series.
    static member inline (~-) series =
        Internal.negate series

    /// Adds the given power series.
    static member inline (+)(seriesF : PowerSeries<'T>, seriesG : PowerSeries<'T>) =
        Internal.add seriesF seriesG

    /// Adds the given constant value to the given power series.
    static member inline (+)(value : 'T, series : PowerSeries<'T>) =
        (Internal.constant value) + series

    /// Subtracts the given power series from the given constant value.
    static member inline (-)(seriesF : PowerSeries<'T>, seriesG : PowerSeries<'T>) =
        Internal.sub seriesF seriesG

    /// Subtracts the given power series from the given constant value.
    static member inline (-)(value : 'T, series : PowerSeries<'T>) =
        (Internal.constant value) - series

    /// Multiplies the given power series.
    static member inline (*)(seriesF : PowerSeries<'T>, seriesG : PowerSeries<'T>) =
        Internal.mult seriesF seriesG

    /// Multiplies the given power series by the given constant.
    static member inline (*)(value : 'T, series : PowerSeries<'T>) =
        (Internal.constant value) * series

    /// Divides the given power series.
    static member inline (/)(seriesF : PowerSeries<'T>, seriesG : PowerSeries<'T>) =
        Internal.div seriesF seriesG

    /// Divides the given constant by the given power series.
    static member inline (/)(value : 'T, series : PowerSeries<'T>) =
        (Internal.constant value) / series

    /// Raises the given power series to a power.
    static member inline Pow(series, n) =
        Internal.pow n series

module PowerSeries =

    /// Constructs a power series from the given coeffecients.
    let inline ofList ns =
        Internal.ofList ns

    /// Takes a finite number of coeffecients from the given power series.
    let inline take<'T
            when ^T : (static member Zero : ^T)
            and ^T : (static member One : ^T)
            and ^T : (static member (+) : ^T * ^T -> ^T)
            and ^T : (static member (*) : ^T * ^T -> ^T)
            and ^T : (static member (/) : ^T * ^T -> ^T)
            and ^T : (static member (~-) : ^T -> ^T)
            and ^T : equality> n series =
        let rec loop n (f : 'T :: fs) =
            if n <= 0 then
                []
            else
                List.Cons(f, fs.Value |> loop (n-1))
        loop n series

    /// Display string.
    let inline toString series =
        series
            |> take 3
            |> sprintf "%A, ..."

    /// Composes two power series: F(G).
    let inline compose seriesF seriesG =
        let rec loop (f : 'T :: fs) (g : 'T :: gs) =
            if g = GenericZero<'T> then
                f :: lazy (gs.Value * (loop fs.Value (g :: gs)))
            else
                raise <| NotSupportedException()
        loop seriesF seriesG
    
    /// Reverts the given power series. (Finds its inverse.)
    let inline revert series =
        let rec loop (f : 'T :: fs) =
            if f = GenericZero<'T> then
                let rec rs =
                    GenericZero<'T> :: lazy (PowerSeries.One / (compose fs.Value rs))
                rs
            else
                raise <| NotSupportedException()
        loop series

    /// Answers the derivative of the given power series.
    let inline deriv (_ :: fs) =
        let rec deriv1 (g : 'T :: gs) n =
            (n * g) :: lazy (deriv1 gs.Value (n + GenericOne<'T>))
        deriv1 fs.Value GenericOne<'T>

    /// Answers the integral of the given power series.
    let inline private lazyIntegral (fs : Lazy<_>) =
        let rec int1 (g : 'T :: gs) n : PowerSeries<'T> =
            (g / n) :: lazy (int1 gs.Value (n + GenericOne<'T>))
        GenericZero<'T> :: lazy (int1 fs.Value GenericOne<'T>)

    /// Answers the integral of the given power series.
    let inline integral series =
        lazyIntegral (lazy series)

    /// Evaluates the given series for the given value, using the given
    /// number of terms.
    let inline eval n (x : 'T) series =
        let rec loop n (f : 'T :: fs) =
            if n <= 0 then
                GenericZero<'T>
            else
                f + (Internal.scale x fs.Value |> loop (n - 1))
        series |> loop n

    /// Exponential function.
    let exp =
        let rec lazyExp =
            lazy (PowerSeries<BigRational>.One + (lazyIntegral lazyExp))
        lazyExp.Value

    /// Sine and cosine functions.
    let sin, cos =
        let rec lazySin =
            lazy (lazyIntegral lazyCos)
        and lazyCos =
            lazy (PowerSeries<BigRational>.One - (lazyIntegral lazySin))
        lazySin.Value, lazyCos.Value

    /// Tangent function.
    let tan = sin / cos

    /// Answers the square root of the given series.
    let inline sqrt<'T
            when ^T : (static member Zero : ^T)
            and ^T : (static member One : ^T)
            and ^T : (static member (+) : ^T * ^T -> ^T)
            and ^T : (static member (*) : ^T * ^T -> ^T)
            and ^T : (static member (/) : ^T * ^T -> ^T)
            and ^T : (static member (~-) : ^T -> ^T)
            and ^T : equality> series =
        let fail () = failwith "Can't compute square root"
        let rec loop (f : 'T :: fs) =
            if f = GenericZero<'T> then
                let (f :: fs) = fs.Value
                if f = GenericZero<'T> then
                    f :: lazy (loop fs.Value)
                else fail ()
            elif f = GenericOne<'T> then
                let rec lazyQs : Lazy<PowerSeries<'T>> =
                    let lazyDiv =
                        let num = deriv (GenericOne<'T> :: fs)
                        let lazyDen =
                            lazy (lazyQs.Value + lazyQs.Value)
                        lazy (num / lazyDen.Value)
                    lazy (PowerSeries<'T>.One + lazyIntegral lazyDiv)
                lazyQs.Value
            else fail ()
        series |> loop
