using Arky.Dom.Abstractions;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;

    public class Entity : IEntity
    {
        [Key]
        public Guid Id { get; set; }

        public virtual bool Validate()
        {
            Id = Guid.NewGuid();
            return true;
            //{
            //    if (Id == Guid.Empty) throw new ArgumentException("Id cannot be empty.", nameof(Id)
            //});
        }

#if DEBUG
        public override string ToString()
        {
            return JsonSerializer.Serialize(this, GetType(), new JsonSerializerOptions { MaxDepth = 3 });
        }
#endif
    }