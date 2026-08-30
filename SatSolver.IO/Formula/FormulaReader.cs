using SatSolver.Core.Formulas;

namespace SatSolver.IO.Formula;

public class FormulaReader
{
    public Core.Formulas.Formula Read(TextReader reader)
    {
        ArgumentNullException.ThrowIfNull(reader);

        var parser = new Parser(reader.ReadToEnd());
        var formula = parser.ReadFormula();
        parser.EnsureEndOfInput();
        return formula;
    }

    private sealed class Parser(string input)
    {
        private int _position;

        public Core.Formulas.Formula ReadFormula()
        {
            SkipWhitespace();
            if (IsEnd)
                throw Error("Expected a formula.");

            if (input[_position] != '(')
                return new Variable(ReadIdentifier());

            _position++;
            var operation = ReadIdentifier();
            Core.Formulas.Formula formula = operation switch
            {
                "and" => new And(ReadFormula(), ReadFormula()),
                "or" => new Or(ReadFormula(), ReadFormula()),
                "not" => ReadNot(),
                _ => throw Error($"Unknown operation '{operation}'.")
            };

            SkipWhitespace();
            if (IsEnd || input[_position] != ')')
                throw Error("Expected ')'.");

            _position++;
            return formula;
        }

        public void EnsureEndOfInput()
        {
            SkipWhitespace();
            if (!IsEnd)
                throw Error("Expected end of input.");
        }

        private Not ReadNot()
        {
            SkipWhitespace();
            if (IsEnd || input[_position] == '(')
                throw Error("Expected a variable after 'not'.");

            return new Not(new Variable(ReadIdentifier()));
        }

        private string ReadIdentifier()
        {
            SkipWhitespace();
            if (IsEnd || !char.IsLetter(input[_position]))
                throw Error("Expected an identifier starting with a letter.");

            var start = _position++;
            while (!IsEnd && char.IsLetterOrDigit(input[_position]))
                _position++;

            return input[start.._position];
        }

        private void SkipWhitespace()
        {
            while (!IsEnd && char.IsWhiteSpace(input[_position]))
                _position++;
        }

        private bool IsEnd => _position >= input.Length;

        private FormatException Error(string message) =>
            new($"{message} (at character {_position}).");
    }
}
