using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FiguraServer.Server.WebSockets.Messages
{
    public static class MessageIDs
    {
        //--------------------HANDLERS--------------------
        #region HANDLERS

        #region AVATARS
        //Avatar messages are between ID 0-19
        public const sbyte AVATAR_REQUEST_HANDLER_ID = sbyte.MinValue;
        public const sbyte AVATAR_UPLOAD_HANDLER_ID = sbyte.MinValue + 1;
        public const sbyte AVATAR_DELETE_HANDLER_ID = sbyte.MinValue + 2;

        #endregion //AVATARS

        #region USERS
        //User messages are between ID 20-39
        public const sbyte USER_GET_AVATAR_UUID_HANDLER_ID = sbyte.MinValue + 20;
        public const sbyte USER_SET_AVATAR_REQUEST_HANDLER_ID = sbyte.MinValue + 21;
        public const sbyte USER_DELETE_CURRENT_AVATAR_REQUEST_HANDLER_ID = sbyte.MinValue + 22;
        public const sbyte USER_GET_CURRENT_AVATAR_REQUEST_HANDLER_ID = sbyte.MinValue + 23;
        public const sbyte USER_GET_CURRENT_AVATAR_HASH_REQUEST_HANDLER_ID = sbyte.MinValue + 24;


        #endregion //USERS

        #region PACK
        //Pack messages are between ID 40-59

        #endregion

        public const sbyte AUTHENTICATE_REQUEST_HANDLER_ID = sbyte.MinValue + 60;

        #endregion //SENDERS

        //--------------------SENDERS--------------------
        #region SENDERS

        #region AVATARS
        //Avatar messages are between ID 0-19
        public const sbyte AVATAR_PROVIDE_RESPONSE_ID = sbyte.MinValue;
        public const sbyte AVATAR_UPLOAD_RESPONSE_ID = sbyte.MinValue + 1;
        public const sbyte AVATAR_DELETE_RESPONSE_ID = sbyte.MinValue + 2;

        #endregion //AVATARS

        #region USERS
        //User messages are between ID 20-39
        public const sbyte USER_GET_AVATAR_UUID_PROVIDE_RESPONSE_ID = sbyte.MinValue + 20;
        public const sbyte USER_AVATAR_HASH_PROVIDE_RESPONSE_ID = sbyte.MinValue + 21;

        #endregion //USERS

        public const sbyte AUTHENTICATE_RESPONSE_ID = sbyte.MinValue + 40;

        #endregion //SENDERS
    }
}
