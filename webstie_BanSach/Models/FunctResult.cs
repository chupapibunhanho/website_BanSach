namespace webstie_BanSach.Models
{
    public enum EnumErrCode
    {
        // 0. General
        Error = -1,
        Empty = 0,
        Success = 1,
        Fail = 2,

        // 1. Authentication & Authorization
        NotLogin = 100,
        InvalidToken = 101,
        TokenExpired = 102,
        Unauthorized = 103,
        Forbidden = 104,

        // 2. Validation
        InvalidInput = 200,
        MissingInput = 201,
        InvalidFormat = 202,
        InvalidLength = 203,
        InvalidValue = 204,

        // 3. Data
        NotExist = 300,
        Existent = 301,
        Duplicate = 302,
        DataNotFound = 303,
        DataConflict = 304,
        DataInvalid = 305,

        // 4. Business
        BusinessError = 400,
        InsufficientQuantity = 401,
        OutOfStock = 402,
        InsufficientBalance = 403,
        OperationNotAllowed = 404,
        OrderCannotCancel = 405,

        // 5. File
        FileNotFound = 500,
        FileTooLarge = 501,
        InvalidFileType = 502,
        FileUploadFailed = 503,

        // 6. System
        InternalServerError = 900,
        DatabaseError = 901,
        ServiceUnavailable = 902,
        Timeout = 903,
        ExternalServiceError = 904
    }

    public class FunctResult<T>
    {
        public int ErrCode { get; set; }
        public string ErrDesc { get; set; }
        public T Data { get; set; }

        public FunctResult()
        {
            ErrCode = (int)EnumErrCode.Error;
            ErrDesc = "Có lỗi xảy ra";
            Data = default(T);
        }
    }
}