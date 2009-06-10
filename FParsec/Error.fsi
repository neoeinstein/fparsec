﻿// Copyright (c) Stephan Tolksdorf 2007-2009
// License: Simplified BSD License. See accompanying documentation.

module FParsec.Error

val invalidPos : Pos

#nowarn "61" // "The containing type can use 'null' as a representation value for its nullary union case. This member will be compiled as a static member."

type ErrorMessage =
     /// Something expected could not be parsed.
     /// This error can be generated with the labeling operator <?>.
     /// The constructor argument is a label describing what was expected.
     | Expected of string
     /// Something unexpected was in the input.
     /// This error is primarily generated by the `notFollowedByL`,
     /// `notFollowedByChar` and `notFollowedByString` parser primitives.
     | Unexpected of string
     /// An error that does not fit into the other categories.
     /// This error can be generated with the `fail` and `failFatally` primitives.
     /// The constructor argument is a string containing the error message.
     | Message of string
     /// A "compound" failed to parse.
     /// Primarily generated by the compound-labelling operator <??>.
     /// The constructor arguments are a label for the compound parser and
     /// the error (Pos + ErrorMessageList) that caused the compound to fail to parse.
     | CompoundError of string * Pos * ErrorMessageList
     /// The parser backtracked after an error occurred.
     /// This error is primarily generated by the `attempt`, `>>?` and `.>>?` primitives.
     /// The constructor argument is the error (Pos + ErrorMessageList) that occurred
     /// before the backtracking.
     | BacktrackPoint of Pos * ErrorMessageList
     /// An error that can be used for application specific error data.
     /// Users will have to define their own error printer, as the default printer
     /// does not print out `OtherError`s.
     /// ATTENTION: The constructor argument is expected to be comparable by
     /// F#'s structural comparison function `compare`. `OtherError`s with arguments
     /// that are not comparable are ignored by the `ErrorMessageList` methods.
     | OtherError of obj

/// A list of error messages. The order of error messages in the list carries no meaning.
/// Duplicate error messages and empty `Expected`, `Unexpected` and `Message`
/// messages have no influence on ordering or equality comparison and are ignored when
/// the list is converted to a set.
and [<CompilationRepresentation(CompilationRepresentationFlags.UseNullAsTrueValue);
      StructuralEquality(false); StructuralComparison(false)>]
    ErrorMessageList  =
    | AddErrorMessage of ErrorMessage * ErrorMessageList
    | NoErrorMessages
    with
        member ToSet:   unit -> Set<ErrorMessage>

        static member OfSeq: seq<ErrorMessage> -> ErrorMessageList

        [<CompilationRepresentation(CompilationRepresentationFlags.Instance)>]
        override Equals: obj -> bool
        [<CompilationRepresentation(CompilationRepresentationFlags.Instance)>]
        override GetHashCode: unit -> int
        interface System.IComparable

        static member internal GetDebuggerDisplay: ErrorMessageList -> string

/// `expectedError label` is equivalent to `AddErrorMessage(Expected(label), NoErrorMessages)`.
val expectedError:   string -> ErrorMessageList
/// `unexpectedError label` is equivalent to `AddErrorMessage(Unexpected(label), NoErrorMessages)`.
val unexpectedError: string -> ErrorMessageList
/// `messageError msg` is equivalent to `AddErrorMessage(Message(msg), NoErrorMessages)`.
val messageError:    string -> ErrorMessageList
/// `otherError obj` is equivalent to `AddErrorMessage(OtherError(obj: obj), NoErrorMessages)`.
val otherError:      obj    -> ErrorMessageList

/// `backtrackError state error` is a variant of `AddErrorMessage(BacktrackPoint(state.Pos, error), NoErrorMessages)`,
/// that only wraps the error in a `BacktrackPoint` if it isn't already a `BacktrackPoint`.
val backtrackError:            State<'u> -> ErrorMessageList -> ErrorMessageList

/// `compoundError label state error` is a variant of `AddErrorMessage(CompoundError(label, state.Pos, error), NoErrorMessages)`
/// that uses the original error position instead of `state.Pos` in case `error` is a `BacktrackPoint`.
val compoundError:   string -> State<'u> -> ErrorMessageList -> ErrorMessageList


/// `concatErrorMessages error1 error2` concatenates the two error message lists `error1` and `error2`.
val concatErrorMessages: ErrorMessageList -> ErrorMessageList -> ErrorMessageList

/// `mergeErrors error1 error2` is an optimized variant of `concatErrorMessages error1 error2`
/// that avoids the call to concatErrorMessages if `error1` is empty (= `NoErrorMessages`).
val inline mergeErrors: ErrorMessageList -> ErrorMessageList -> ErrorMessageList

/// `mergeErrorsIfNeeded oldState oldError newState newError` is equivalent to
/// `if newState <> State then newError else mergeErrors oldError newError`.
val inline mergeErrorsIfNeeded:    State<'u> -> ErrorMessageList
                                -> State<'u> -> ErrorMessageList -> ErrorMessageList

/// `mergeErrorsIfNeeded veryOldState veryOldError oldState oldError newState newError` is equivalent to
/// `mergeErrorsIfNeeded oldState (mergeErrorsIfNeeded veryOldState veryOldError oldState oldError) newState newError`.
val inline mergeErrorsIfNeeded3:    State<'u> -> ErrorMessageList
                                 -> State<'u> -> ErrorMessageList
                                 -> State<'u> -> ErrorMessageList -> ErrorMessageList

/// Represents a simple container type that brings together the position and error messages of a parser error.
[<Sealed>]
type ParserError =
    new: Pos * ErrorMessageList -> ParserError

    member Pos:   Pos
    member Error: ErrorMessageList

    override ToString: unit -> string

    /// Returns a string representation of the ParserError.
    /// For each error location the printed position information is augmented with the line of
    /// text surrounding the error position together with a '^'-marker pointing to the exact location
    /// of the error in the input stream.
    /// The given CharStream must contain the original input for which the parser error was generated.
    /// Newlines in the returned string are represented by System.Environment.Newline.
    member ToString: streamWhereErrorOccurred: CharStream -> string

    /// Writes a string representation of the ParserError to the given TextWriter value.
    ///
    /// The format of the position information can be customized by specifying the positionPrinter
    /// argument. The given function is expected to print a representation of the passed Pos value
    /// to the passed TextWriter value. If possible, it should indent text lines with the passed string
    /// and take into account the maximum column count (including indention) passed as the last argument.
    ///
    /// If a value for the optional CharStream argument is given, the printed position
    /// information is augmented with the the line of text surrounding the error position
    /// together with a '^'-marker pointing to the exact location of the error in the input stream.
    /// The given CharStream must contain the original input for which the parser error was generated.
    member WriteTo:   textWriter: System.IO.TextWriter
                    * ?positionPrinter: (System.IO.TextWriter -> Pos -> string -> int -> unit)
                    * ?columnWidth: int * ?initialIndention: string * ?indentionIncrement: string
                    * ?streamWhereErrorOccurred: CharStream
                    -> unit

/// Prints the line of text surrounding the given index in the stream and
/// marks the position of the index with a caret (^) on a second line.
/// The output is indented with the given string and is restricted to the
/// given columWidth (including the indention).
val printErrorLine: CharStream -> index: int64 -> System.IO.TextWriter -> indentionString: string -> columnWidth: int -> unit


/// Internal helper function that needs to be public because it gets called from inline functions.
val _raiseInfiniteLoopException: string -> State<'u> -> 'a