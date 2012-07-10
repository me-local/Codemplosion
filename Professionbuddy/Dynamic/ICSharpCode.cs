/*  ----------------------------------------------------------------------------
 *  CODEMPLOSION.com
 *  ----------------------------------------------------------------------------
 *  File:       ICSharpCode.cs
 *  Author:     starz and team
 *  License:    Creative Commons Attribution-NonCommercial-ShareAlike (http://creativecommons.org/licenses/by-nc-sa/3.0/)
 *  ----------------------------------------------------------------------------
 */

using System;
using HighVoltz.Composites;

namespace HighVoltz.Dynamic
{
    public enum CsharpCodeType
    {
        BoolExpression,
        Statements,
        Declaration,
        Expression
    }

    public interface ICSharpCode
    {
        int CodeLineNumber { get; set; }
        string CompileError { get; set; }
        CsharpCodeType CodeType { get; }
        string Code { get; }
        Delegate CompiledMethod { get; set; }
        IPBComposite AttachedComposite { get; }
    }
}