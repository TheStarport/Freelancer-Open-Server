using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FOS_ng
{
    class Chat
    {

        /// <summary>
        ///     Message shown for /help command
        /// </summary>
        private string _userHelpMsg;

        /// <summary>
        ///     Message shown for .help command
        /// </summary>
        private string _adminHelpMsg;

        /// <summary>
        ///     Message shown as chat text every 10 minutes or so.
        /// </summary>
        private string _bannerMsg;

        /// <summary>
        ///     Message shown on first connection to server.
        /// </summary>
        public string _introMsg;

    }
}
