//------------------------------------------------------------------------------
// <copyright file="RegexReplacement.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>                                                                
//------------------------------------------------------------------------------

// The RegexReplacement class represents a substitution string for
// use when using regexs to search/replace, etc. It's logically
// a sequence intermixed (1) constant strings and (2) group numbers.

namespace MonoDevelop.Ide.Editor.Highlighting.RegexEngine {

	using System;
	using System.Text;
	using System.Collections;
	using System.Collections.Generic;
	using MonoDevelop.Core.Text;

	[Obsolete ("Old editor")]
	internal sealed class RegexReplacement
	{
        /*
         * Since RegexReplacement shares the same parser as Regex,
         * the constructor takes a RegexNode which is a concatenation
         * of constant strings and backreferences.
         */
#if SILVERLIGHT
        internal RegexReplacement(String rep, RegexNode concat, Dictionary<Int32, Int32> _caps) {
#else
        internal RegexReplacement(String rep, RegexNode concat, Hashtable _caps) {
#endif
            StringBuilder sb;
            List<String> strings;
            List<Int32> rules;
            int slot;

            _rep = rep;

            if (concat.Type() != RegexNode.Concatenate)
                throw new ArgumentException();

            sb = new StringBuilder();
            strings = new List<String>();
            rules = new List<Int32>();

            for (int i = 0; i < concat.ChildCount(); i++) {
                RegexNode child = concat.Child(i);

                switch (child.Type()) {
                    case RegexNode.Multi:
                        sb.Append(child._str);
                        break;
                    case RegexNode.One:
                        sb.Append(child._ch);
                        break;
                    case RegexNode.Ref:
                        if (sb.Length > 0) {
                            rules.Add(strings.Count);
                            strings.Add(sb.ToString());
                            sb.Length = 0;
                        }
                        slot = child._m;

                        if (_caps != null && slot >= 0)
                            slot = (int)_caps[slot];

                        rules.Add(-Specials - 1 - slot);
                        break;
                    default:
                        throw new ArgumentException();
                }
            }

            if (sb.Length > 0) {
                rules.Add(strings.Count);
                strings.Add(sb.ToString());
            }

            _strings = strings; 
            _rules = rules; 
        }

        internal String _rep;
        internal List<String>  _strings;          // table of string constants
        internal List<Int32>  _rules;            // negative -> group #, positive -> string #

        // constants for special insertion patterns

        internal const int Specials       = 4;
        internal const int LeftPortion    = -1;
        internal const int RightPortion   = -2;
        internal const int LastGroup      = -3;
        internal const int WholeString    = -4;

        /*       
         * Given a Match, emits into the StringBuilder the evaluated
         * substitution pattern.
         */
        private void ReplacementImpl(StringBuilder sb, Match match) {
            for (int i = 0; i < _rules.Count; i++) {
                int r = _rules[i];
                if (r >= 0)   // string lookup
                    sb.Append(_strings[r]);
                else if (r < -Specials) // group lookup
                    sb.Append(match.GroupToStringImpl(-Specials - 1 - r));
                else {
                    switch (-Specials - 1 - r) { // special insertion patterns
                        case LeftPortion:
                            sb.Append(match.GetLeftSubstring());
                            break;
                        case RightPortion:
                            sb.Append(match.GetRightSubstring());
                            break;
                        case LastGroup:
                            sb.Append(match.LastGroupToStringImpl());
                            break;
                        case WholeString:
                            sb.Append(match.GetOriginalString());
                            break;
                    }
                }
            }
        }

        /*       
         * Given a Match, emits into the List<String> the evaluated
         * Right-to-Left substitution pattern.
         */
		private void ReplacementImplRTL(List<string> al, Match match) {
            for (int i = _rules.Count - 1; i >= 0; i--) {
                int r = _rules[i];
                if (r >= 0)  // string lookup
                    al.Add(_strings[r]);
                else if (r < -Specials) // group lookup
                    al.Add(match.GroupToStringImpl(-Specials - 1 - r));
                else {
                    switch (-Specials - 1 - r) { // special insertion patterns
                        case LeftPortion:
						al.Add(match.GetLeftSubstring().ToString ());
                            break;
                        case RightPortion:
						al.Add(match.GetRightSubstring().ToString ());
                            break;
                        case LastGroup:
                            al.Add(match.LastGroupToStringImpl());
                            break;
                        case WholeString:
						al.Add(match.GetOriginalString().ToString ());
                            break;
                    }
                }
            }
        }

        /*
         * The original pattern string
         */
        internal String Pattern {
            get {
                return _rep;
            }
        }

        /*
         * Returns the replacement result for a single match
         */
        internal String Replacement(Match match) {
            StringBuilder sb = new StringBuilder();

            ReplacementImpl(sb, match);

            return sb.ToString();
        }

        /*
         * Three very similar algorithms appear below: replace (pattern),
         * replace (evaluator), and split.
         */


        /*
         * Replaces all ocurrances of the regex in the string with the
         * replacement pattern.
         *
         * Note that the special case of no matches is handled on its own:
         * with no matches, the input string is returned unchanged.
         * The right-to-left case is split out because StringBuilder
         * doesn't handle right-to-left string building directly very well.
         */
        internal String Replace(Regex regex, string input, int count, int startat) {
            Match match;

            if (count < -1)
                throw new ArgumentOutOfRangeException("count");
            if (startat < 0 || startat > input.Length) 
                throw new ArgumentOutOfRangeException("startat");

            if (count == 0)
				return input.ToString ();

            match = regex.Match(input, startat);
            if (!match.Success) {
				return input.ToString ();
            }
            else {
                StringBuilder sb;

                if (!regex.RightToLeft) {
                    sb = new StringBuilder();
                    int prevat = 0;

                    do {
                        if (match.Index != prevat)
							sb.Append (input.Substring (prevat, match.Index - prevat));

                        prevat = match.Index + match.Length;
                        ReplacementImpl(sb, match);
                        if (--count == 0)
                            break;

                        match = match.NextMatch();
                    } while (match.Success);

                    if (prevat < input.Length)
						sb.Append (input.Substring (prevat, input.Length - prevat));
                }
                else {
                    List<String> al = new List<String>();
                    int prevat = input.Length;

                    do {
                        if (match.Index + match.Length != prevat)
							al.Add (input.Substring (match.Index + match.Length, prevat - match.Index - match.Length));

                        prevat = match.Index;
                        ReplacementImplRTL(al, match);
                        if (--count == 0)
                            break;

                        match = match.NextMatch();
                    } while (match.Success);

                    sb = new StringBuilder();

                    if (prevat > 0)
						sb.Append (input.Substring (0, prevat));

                    for (int i = al.Count - 1; i >= 0; i--) {
                        sb.Append(al[i]);
                    }
                }

                return sb.ToString();
            }
        }

        /*
         * Replaces all ocurrances of the regex in the string with the
         * replacement evaluator.
         *
         * Note that the special case of no matches is handled on its own:
         * with no matches, the input string is returned unchanged.
         * The right-to-left case is split out because StringBuilder
         * doesn't handle right-to-left string building directly very well.
         */
        internal static String Replace(MatchEvaluator evaluator, Regex regex,
                                       string input, int count, int startat) {
            Match match;

            if (evaluator == null)
                throw new ArgumentNullException(nameof(evaluator));
            if (count < -1)
                throw new ArgumentOutOfRangeException(nameof(count));
            if (startat < 0 || startat > input.Length)
                throw new ArgumentOutOfRangeException(nameof(startat));

            if (count == 0)
				return input.ToString ();

            match = regex.Match(input, startat);

            if (!match.Success) {
				return input.ToString ();
            }
            else {
                StringBuilder sb;

                if (!regex.RightToLeft) {
                    sb = new StringBuilder();
                    int prevat = 0;

                    do {
                        if (match.Index != prevat)
							sb.Append (input.Substring (prevat, match.Index - prevat));

                        prevat = match.Index + match.Length;

                        sb.Append(evaluator(match));

                        if (--count == 0)
                            break;

                        match = match.NextMatch();
                    } while (match.Success);

                    if (prevat < input.Length)
						sb.Append (input.Substring (prevat, input.Length - prevat));
                }
                else {
                    List<String> al = new List<String>();
                    int prevat = input.Length;

                    do {
                        if (match.Index + match.Length != prevat)
							al.Add (input.Substring (match.Index + match.Length, prevat - match.Index - match.Length));

                        prevat = match.Index;

                        al.Add(evaluator(match));

                        if (--count == 0)
                            break;

                        match = match.NextMatch();
                    } while (match.Success);

                    sb = new StringBuilder();

                    if (prevat > 0)
						sb.Append (input.Substring (0, prevat));

                    for (int i = al.Count - 1; i >= 0; i--) {
                        sb.Append(al[i]);
                    }
                }

                return sb.ToString();
            }
        }

        /*
         * Does a split. In the right-to-left case we reorder the
         * array to be forwards.
         */
        internal static string[] Split(Regex regex, string input, int count, int startat) {
            Match match;
			string [] result;

            if (count < 0)
                throw new ArgumentOutOfRangeException("count");

            if (startat < 0 || startat > input.Length) 
                throw new ArgumentOutOfRangeException("startat");
                
            if (count == 1) {
				result = new string[1];
                result[0] = input;
                return result;
            }

            count -= 1;

            match = regex.Match(input, startat);

            if (!match.Success) {
				result = new string[1];
                result[0] = input;
                return result;
            }
            else {
				List<string> al = new List<string>();

                if (!regex.RightToLeft) {
                    int prevat = 0;

                    for (;;) {
						al.Add (input.Substring(prevat, match.Index - prevat));

                        prevat = match.Index + match.Length;
                        
                        // add all matched capture groups to the list.
                        for (int i=1; i<match.Groups.Count; i++) {
                            if (match.IsMatched(i))
								al.Add(match.Groups[i].ToString ());
                        }

                        if (--count == 0)
                            break;

                        match = match.NextMatch();

                        if (!match.Success)
                            break;
                    }

					al.Add (input.Substring(prevat, input.Length - prevat));
                }
                else {
                    int prevat = input.Length;

                    for (;;) {
						al.Add (input.Substring(match.Index + match.Length, prevat - match.Index - match.Length));

                        prevat = match.Index;

                        // add all matched capture groups to the list.
                        for (int i=1; i<match.Groups.Count; i++) {
                            if (match.IsMatched(i))
								al.Add(match.Groups[i].ToString());
                        }

                        if (--count == 0)
                            break;

                        match = match.NextMatch();

                        if (!match.Success)
                            break;
                    }

					al.Add (input.Substring(0, prevat));

                    al.Reverse(0, al.Count);
                }

                return al.ToArray();
            }
        }
    }

}
