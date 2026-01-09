using System.Collections.Generic;
using rpg_deneme.Data;
using rpg_deneme.Models;

namespace rpg_deneme.Business;

/// <summary>
/// Karakter yönetimi ile ilgili iş kurallarını (business logic) yöneten sınıf.
/// </summary>
public class CharacterManager
{
    private readonly CharacterRepository _repo = new CharacterRepository();

    /// <summary>
    /// Mevcut tüm karakterleri döner.
    /// </summary>
    public List<CharacterModel> GetCharacters()
    {
        return _repo.GetCharacters();
    }

    /// <summary>
    /// İş kurallarını kontrol ederek yeni bir karakter oluşturur.
    /// </summary>
    public (bool Success, string Message) CreateCharacter(CharacterModel model)
    {
        if (model.Name.Length < 3 || model.Name.Length > 16)
        {
            return (Success: false, Message: "Karakter ismi 3-16 karakter arasında olmalıdır.");
        }

        if (!_repo.IsNameAvailable(model.Name))
        {
            return (Success: false, Message: "Bu isim zaten kullanılmaktadır.");
        }

        List<CharacterModel> currentChars = _repo.GetCharacters();
        if (currentChars.Count >= 8)
        {
            return (Success: false, Message: "En fazla 8 karakter oluşturabilirsiniz.");
        }

        bool result = _repo.CreateCharacter(model);
        return result
            ? (Success: true, Message: "Karakter başarıyla oluşturuldu!")
            : (Success: false, Message: "Kayıt sırasında teknik bir hata oluştu.");
    }

    public void UpdateSlotIndex(int characterId, int newSlotIndex)
    {
        _repo.UpdateSlotIndex(characterId, newSlotIndex);
    }
}
