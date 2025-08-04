using System;
using Neo.Cryptography.BN254;

public class MontgomeryDebug
{
    public static void Main()
    {
        Console.WriteLine("=== BN254 Montgomery Form Debug ===");
        
        // Test basic constants
        Console.WriteLine($"Fp.Zero = {Fp.Zero}");
        Console.WriteLine($"Fp.One = {Fp.One}");
        Console.WriteLine($"FpConstants.R = {FpConstants.R}");
        
        // Test basic arithmetic
        var one = Fp.One;
        var oneSquared = one * one;
        Console.WriteLine($"Fp.One * Fp.One = {oneSquared}");
        Console.WriteLine($"Expected: {Fp.One}");
        Console.WriteLine($"Match: {oneSquared == Fp.One}");
        
        // Test addition
        var twoR = Fp.One + Fp.One;
        Console.WriteLine($"Fp.One + Fp.One = {twoR}");
        
        // Test inversion
        if (Fp.One.TryInvert(out var invOne))
        {
            Console.WriteLine($"Inverse of Fp.One = {invOne}");
            Console.WriteLine($"Expected: {Fp.One}");
            Console.WriteLine($"Match: {invOne == Fp.One}");
            
            var product = Fp.One * invOne;
            Console.WriteLine($"Fp.One * inv = {product}");
            Console.WriteLine($"Match with One: {product == Fp.One}");
        }
        
        // Test Scalar arithmetic
        Console.WriteLine("\n=== Scalar Debug ===");
        Console.WriteLine($"Scalar.Zero = {Scalar.Zero}");
        Console.WriteLine($"Scalar.One = {Scalar.One}");
        Console.WriteLine($"ScalarConstants.R = {ScalarConstants.R}");
        
        var scalarOne = Scalar.One;
        var scalarOneSquared = scalarOne * scalarOne;
        Console.WriteLine($"Scalar.One * Scalar.One = {scalarOneSquared}");
        Console.WriteLine($"Expected: {Scalar.One}");
        Console.WriteLine($"Match: {scalarOneSquared == Scalar.One}");
        
        if (scalarOne.TryInvert(out var invScalarOne))
        {
            Console.WriteLine($"Inverse of Scalar.One = {invScalarOne}");
            Console.WriteLine($"Expected: {Scalar.One}");
            Console.WriteLine($"Match: {invScalarOne == Scalar.One}");
            
            var scalarProduct = scalarOne * invScalarOne;
            Console.WriteLine($"Scalar.One * inv = {scalarProduct}");
            Console.WriteLine($"Match with One: {scalarProduct == Scalar.One}");
        }
    }
}