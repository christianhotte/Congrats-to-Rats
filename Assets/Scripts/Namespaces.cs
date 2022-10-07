using System.Collections.Generic;
using UnityEngine;

namespace CustomEnums
{
    /// <summary>
    /// Indicator of unique properties and behavior among follower rats.
    /// </summary>
    public enum RatType { Basic }
    /// <summary>
    /// Denotes targeting behavior of a single RatBoid.
    /// </summary>
    public enum RatBehavior { Free, TrailFollower, Deployed, Distracted, Projectile }
}