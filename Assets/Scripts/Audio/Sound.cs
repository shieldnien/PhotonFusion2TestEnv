using UnityEngine;

/// <summary>
/// Categorías de sonido que permiten clasificar los diferentes tipos de audio en el juego.
/// </summary>
public enum SoundCategory
{
	/// <summary>
	/// Sonidos de efectos especiales (SFX), como pasos, blandir espada, etc.
	/// </summary>
	SFX,

	/// <summary>
	/// Música de fondo o ambiental.
	/// </summary>
	Music,

	/// <summary>
	/// Sonidos de la interfaz de usuario (UI), como clics de botones o notificaciones.
	/// </summary>
	UI,
	/// <summary>
	/// Sonidos de la interfaz de usuario (UI), como clics de botones o notificaciones.
	/// </summary>
	Voices
}

/// <summary>
/// Representa un sonido individual con un nombre único, un clip de audio y una categoría.
/// </summary>
[System.Serializable]
public class Sound
{
	/// <summary>
	/// Nombre único del sonido, utilizado para identificarlo en el sistema de audio.
	/// </summary>
	public string soundName;

	/// <summary>
	/// Clip de audio asociado al sonido.
	/// </summary>
	public AudioClip clip;

	/// <summary>
	/// Categoría del sonido, utilizada para clasificarlo (SFX, Music, UI).
	/// </summary>
	public SoundCategory category;
}