using UnityEngine;

/// <summary>
/// Display a field as read-only in the inspector.
/// CustomPropertyDrawers will not work when this attribute is used.
/// </summary>
/// <seealso cref="BeginReadOnlyInspGroupAttribute"/>
/// <seealso cref="EndReadOnlyInspGroupAttribute"/>
public class ReadOnlyInspAttribute : PropertyAttribute { }

/// <summary>
/// Display one or more fields as read-only in the inspector.
/// Use <see cref="EndReadOnlyInspGroupAttribute"/> to close the group.
/// Works with CustomPropertyDrawers.
/// </summary>
/// <seealso cref="EndReadOnlyInspGroupAttribute"/>
/// <seealso cref="ReadOnlyInspAttribute"/>
public class BeginReadOnlyInspGroupAttribute : PropertyAttribute { }

/// <summary>
/// Use with <see cref="BeginReadOnlyInspGroupAttribute"/>.
/// Close the read-only group and resume editable fields.
/// </summary>
/// <seealso cref="BeginReadOnlyInspGroupAttribute"/>
/// <seealso cref="ReadOnlyInspAttribute"/>
public class EndReadOnlyInspGroupAttribute : PropertyAttribute { }