namespace CoreLang.Semantic.Types
{
    public class TypeSymbol
    {
        public string Name { get; }
        public bool IsNullable { get; }

        public TypeSymbol(string name, bool isNullable = false)
        {
            Name = name;
            IsNullable = isNullable;
        }

        public virtual bool IsNumeric() => this == BuiltInTypes.Int || this == BuiltInTypes.Float;
        public virtual bool IsBoolean() => this == BuiltInTypes.Bool;
        public virtual bool IsString() => this == BuiltInTypes.String;
        public virtual bool IsArray() => false;

        /// <summary>
        /// Determines if <paramref name="other"/> can be assigned to a variable of this type.
        /// </summary>
        public virtual bool IsAssignableFrom(TypeSymbol other)
        {
            if (other == BuiltInTypes.Null)
                return IsNullable;

            if (this == other)
                return true;

            // Nullable version accepts non-nullable of same base
            if (IsNullable && !other.IsNullable && BaseName() == other.BaseName())
                return true;

            // Numeric promotion: i -> f allowed, f -> i NOT allowed
            if (this == BuiltInTypes.Float && other == BuiltInTypes.Int)
                return true;

            // Array assignability
            if (IsArray() && other.IsArray())
            {
                var thisArr = (ArrayTypeSymbol)this;
                var otherArr = (ArrayTypeSymbol)other;
                return thisArr.ElementType.IsAssignableFrom(otherArr.ElementType);
            }

            return false;
        }

        /// <summary>
        /// Returns the base name without nullable marker for comparison purposes.
        /// </summary>
        private string BaseName()
        {
            return Name;
        }

        public TypeSymbol AsNullable()
        {
            if (IsNullable) return this;
            if (IsArray()) return new ArrayTypeSymbol(((ArrayTypeSymbol)this).ElementType, true);
            return new TypeSymbol(Name, true);
        }

        public override string ToString() => IsNullable ? $"{Name}?" : Name;

        public override bool Equals(object? obj)
        {
            if (obj is TypeSymbol other)
                return Name == other.Name && IsNullable == other.IsNullable;
            return false;
        }

        public override int GetHashCode() => HashCode.Combine(Name, IsNullable);
    }
}
