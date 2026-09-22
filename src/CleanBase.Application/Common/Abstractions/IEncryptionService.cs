namespace CleanBase.Application.Common.Abstractions;

public interface IEncryptionService
{
    string Encrypt(string plaintext);
    string Decrypt(string encryptedPayload);
}
