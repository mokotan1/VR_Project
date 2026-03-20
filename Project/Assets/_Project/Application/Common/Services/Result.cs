using System;

namespace VRProject.Application.Common.Services
{
    public sealed class Result
    {
        public bool IsSuccess { get; }
        public string Error { get; }
        public bool IsFailure => !IsSuccess;

        private Result(bool isSuccess, string error)
        {
            if (isSuccess && !string.IsNullOrEmpty(error))
                throw new ArgumentException("Success result cannot have an error message.", nameof(error));
            if (!isSuccess && string.IsNullOrEmpty(error))
                throw new ArgumentException("Failure result must have an error message.", nameof(error));

            IsSuccess = isSuccess;
            Error = error;
        }

        public static Result Success() => new(true, null);
        public static Result Failure(string error) => new(false, error);
        public static Result<T> Success<T>(T value) => Result<T>.Success(value);
        public static Result<T> Failure<T>(string error) => Result<T>.Failure(error);
    }

    public sealed class Result<T>
    {
        public bool IsSuccess { get; }
        public string Error { get; }
        public bool IsFailure => !IsSuccess;

        private readonly T _value;
        public T Value
        {
            get
            {
                if (IsFailure)
                    throw new InvalidOperationException(
                        $"Cannot access Value of a failed result. Error: {Error}");
                return _value;
            }
        }

        private Result(bool isSuccess, T value, string error)
        {
            IsSuccess = isSuccess;
            _value = value;
            Error = error;
        }

        public static Result<T> Success(T value) => new(true, value, null);
        public static Result<T> Failure(string error) => new(false, default, error);
    }
}
