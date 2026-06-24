using UnityEngine;

public class Bullet : MonoBehaviour
{
    public int damage = 5;
    public float despawnDelay = 2f;

    private void OnEnable()
    {
        Invoke(nameof(Despawn), despawnDelay);
    }

    public void Despawn()
    {
        Destroy(gameObject);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        CharacterAbilities character = collision.gameObject.GetComponent<CharacterAbilities>();
        if (character != null)
        {
            character.health -= damage;
            if (character.health < 0)
                Destroy(character.gameObject);
        }
        CancelInvoke(nameof(Despawn));
        Destroy(gameObject);
    }
}
