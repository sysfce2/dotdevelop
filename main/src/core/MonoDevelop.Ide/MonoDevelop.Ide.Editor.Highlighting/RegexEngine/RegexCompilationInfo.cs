//------------------------------------------------------------------------------
// <copyright file="RegexCompilationInfo.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>                                                                
//------------------------------------------------------------------------------

#if !SILVERLIGHT

using System;
using System.Runtime.Serialization;
using System.Runtime.Versioning;

namespace MonoDevelop.Ide.Editor.Highlighting.RegexEngine {
 
    /// <devdoc>
    ///    <para>
    ///       [To be supplied]
    ///    </para>
    /// </devdoc>
    [ Serializable() ]
	[Obsolete ("Old editor")]
    class RegexCompilationInfo { 
        private String           pattern;
        private RegexOptions     options;
        private String           name;
        private String           nspace;
        private bool             isPublic;

        [OptionalField(VersionAdded = 2)]
        private TimeSpan         matchTimeout;

        [OnDeserializing]
        private void InitMatchTimeoutDefaultForOldVersionDeserialization(StreamingContext unusedContext) {
            matchTimeout = Regex.DefaultMatchTimeout;
        }

        /// <devdoc>
        ///    <para>
        ///       [To be supplied]
        ///    </para>
        /// </devdoc>
        public RegexCompilationInfo(String pattern, RegexOptions options, String name, String fullnamespace, bool ispublic)
            : this(pattern, options, name, fullnamespace, ispublic, Regex.DefaultMatchTimeout) {            
        }

        public RegexCompilationInfo(String pattern, RegexOptions options, String name, String fullnamespace, bool ispublic, TimeSpan matchTimeout) {
            Pattern = pattern;
            Name = name;
            Namespace = fullnamespace;
            this.options = options;
            isPublic = ispublic;
            MatchTimeout = matchTimeout;
        }

        /// <devdoc>
        ///    <para>
        ///       [To be supplied]
        ///    </para>
        /// </devdoc>
        public String Pattern {
            get { return pattern; }
            set { 
                if (value == null)
                    throw new ArgumentNullException(nameof(value));
                pattern = value;
            }
        }

        /// <devdoc>
        ///    <para>
        ///       [To be supplied]
        ///    </para>
        /// </devdoc>
        public RegexOptions Options {
            get { return options; }
            set { options = value;}
        }

        /// <devdoc>
        ///    <para>
        ///       [To be supplied]
        ///    </para>
        /// </devdoc>
        public String Name {
            get { return name; }
            set { 
                if (value == null) {
                    throw new ArgumentNullException(nameof(value));
                }
				
                if (value.Length == 0) {
                	throw new ArgumentException("value");					
                }

                name = value;
            }
        }

        /// <devdoc>
        ///    <para>
        ///       [To be supplied]
        ///    </para>
        /// </devdoc>
        public String Namespace {
            get { return nspace; }
            set { 
                if (value == null)
                    throw new ArgumentNullException(nameof(value));
                nspace = value;
            }
        }

        /// <devdoc>
        ///    <para>
        ///       [To be supplied]
        ///    </para>
        /// </devdoc>
        public bool IsPublic {
            get { return isPublic; }
            set { isPublic = value;}
        }

        public TimeSpan MatchTimeout {
            get { return matchTimeout; }
            set { Regex.ValidateMatchTimeout(value); matchTimeout = value; }
        }
    }
}


#endif

