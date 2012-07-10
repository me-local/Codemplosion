/*  ----------------------------------------------------------------------------
 *  CODEMPLOSION.com
 *  ----------------------------------------------------------------------------
 *  File:       SvnBase.cs
 *  Author:     starz and team
 *  License:    Creative Commons Attribution-NonCommercial-ShareAlike (http://creativecommons.org/licenses/by-nc-sa/3.0/)
 *  ----------------------------------------------------------------------------
 */

namespace HighVoltz
{
    public class SvnBase
    {
        private int _rev = -1;

        protected virtual string RevString
        {
            get { return "0"; }
        }

        public int Revision
        {
            get
            {
                if (_rev == -1)
                    int.TryParse(RevString, out _rev);
                return _rev + 1;
            }
        }
    }

    public partial class Svn : SvnBase
    {
    }
}