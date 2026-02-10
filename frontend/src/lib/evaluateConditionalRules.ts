import type {
  ConditionalRuleDto,
  QuestionDefinitionDto,
} from "@/services/questionService";

export type AnswerValue = string | number | boolean | null | undefined;

const enum TokenType {
  Identifier = "Identifier",
  String = "String",
  Number = "Number",
  Boolean = "Boolean",
  And = "And",
  Or = "Or",
  Not = "Not",
  In = "In",
  Equals = "Equals",
  NotEquals = "NotEquals",
  Greater = "Greater",
  GreaterOrEqual = "GreaterOrEqual",
  Less = "Less",
  LessOrEqual = "LessOrEqual",
  LeftParen = "LeftParen",
  RightParen = "RightParen",
  LeftBracket = "LeftBracket",
  RightBracket = "RightBracket",
  Comma = "Comma",
  End = "End",
}

type Token = {
  type: TokenType;
  lexeme: string;
  position: number;
};

type RuleValue =
  | { type: "string"; value: string }
  | { type: "number"; value: number }
  | { type: "boolean"; value: boolean };

type RuleNode = {
  evaluate: (answers: Record<string, AnswerValue>) => boolean;
  collectIds: (ids: Set<string>) => void;
};

export function isQuestionVisible(
  question: QuestionDefinitionDto,
  answers: Record<string, AnswerValue>,
  rules: ConditionalRuleDto[],
): boolean {
  if (!question.conditionalRuleId) return true;

  const rule = rules.find(
    (candidate) => candidate.conditionalRuleId === question.conditionalRuleId,
  );

  if (!rule) return true;

  return evaluateRuleExpression(rule.ruleExpression, answers);
}

export function evaluateRuleExpression(
  expression: string,
  answers: Record<string, AnswerValue>,
): boolean {
  if (!expression || expression.trim().length === 0) {
    throw new Error("Rule expression is required.");
  }

  const parser = new Parser(expression);
  const node = parser.parseExpression();
  parser.ensureEnd();
  return node.evaluate(answers);
}

export function getReferencedQuestionIds(expression: string): Set<string> {
  if (!expression || expression.trim().length === 0) {
    return new Set();
  }

  const parser = new Parser(expression);
  const node = parser.parseExpression();
  parser.ensureEnd();

  const ids = new Set<string>();
  node.collectIds(ids);
  return ids;
}

class Tokenizer {
  private position = 0;

  constructor(private readonly text: string) {}

  nextToken(): Token {
    this.skipWhitespace();

    if (this.position >= this.text.length) {
      return { type: TokenType.End, lexeme: "", position: this.position };
    }

    const start = this.position;
    const current = this.text[this.position];

    switch (current) {
      case "(":
        this.position++;
        return { type: TokenType.LeftParen, lexeme: "(", position: start };
      case ")":
        this.position++;
        return { type: TokenType.RightParen, lexeme: ")", position: start };
      case "[":
        this.position++;
        return { type: TokenType.LeftBracket, lexeme: "[", position: start };
      case "]":
        this.position++;
        return { type: TokenType.RightBracket, lexeme: "]", position: start };
      case ",":
        this.position++;
        return { type: TokenType.Comma, lexeme: ",", position: start };
      case "=":
        if (this.peek("=")) {
          this.position += 2;
          return { type: TokenType.Equals, lexeme: "==", position: start };
        }
        break;
      case "!":
        if (this.peek("=")) {
          this.position += 2;
          return { type: TokenType.NotEquals, lexeme: "!=", position: start };
        }
        break;
      case ">":
        if (this.peek("=")) {
          this.position += 2;
          return {
            type: TokenType.GreaterOrEqual,
            lexeme: ">=",
            position: start,
          };
        }
        this.position++;
        return { type: TokenType.Greater, lexeme: ">", position: start };
      case "<":
        if (this.peek("=")) {
          this.position += 2;
          return { type: TokenType.LessOrEqual, lexeme: "<=", position: start };
        }
        this.position++;
        return { type: TokenType.Less, lexeme: "<", position: start };
      case "'":
      case '"':
        return this.readStringToken();
    }

    if (this.isDigit(current) || (current === "-" && this.peekDigit())) {
      if (this.looksLikeNumber()) {
        return this.readNumberToken();
      }
      return this.readIdentifierOrKeyword();
    }

    if (this.isIdentifierStart(current)) {
      return this.readIdentifierOrKeyword();
    }

    throw new Error(`Unexpected character '${current}' at position ${start}.`);
  }

  private skipWhitespace() {
    while (
      this.position < this.text.length &&
      /\s/.test(this.text[this.position])
    ) {
      this.position++;
    }
  }

  private peek(expected: string) {
    return (
      this.position + 1 < this.text.length &&
      this.text[this.position + 1] === expected
    );
  }

  private peekDigit() {
    return (
      this.position + 1 < this.text.length &&
      /\d/.test(this.text[this.position + 1])
    );
  }

  private isDigit(value: string): boolean {
    return /\d/.test(value);
  }

  private isIdentifierStart(value: string) {
    return /[A-Za-z0-9]/.test(value);
  }

  private looksLikeNumber(): boolean {
    let position = this.position;
    if (this.text[position] === "-") {
      position++;
    }

    let sawDigit = false;
    let sawDot = false;

    while (position < this.text.length) {
      const current = this.text[position];
      if (/\d/.test(current)) {
        sawDigit = true;
        position++;
        continue;
      }
      if (current === "." && !sawDot) {
        sawDot = true;
        position++;
        continue;
      }
      break;
    }

    if (!sawDigit) {
      return false;
    }

    if (position < this.text.length && /[A-Za-z_-]/.test(this.text[position])) {
      return false;
    }

    return true;
  }

  private readStringToken(): Token {
    const quote = this.text[this.position];
    const start = this.position;
    this.position++;

    const buffer: string[] = [];
    while (this.position < this.text.length) {
      const current = this.text[this.position++];
      if (current === "\\") {
        if (this.position >= this.text.length) {
          throw new Error("Unterminated escape sequence in string literal.");
        }
        buffer.push(this.text[this.position++]);
        continue;
      }

      if (current === quote) {
        return {
          type: TokenType.String,
          lexeme: buffer.join(""),
          position: start,
        };
      }

      buffer.push(current);
    }

    throw new Error("Unterminated string literal.");
  }

  private readNumberToken(): Token {
    const start = this.position;
    const buffer: string[] = [];

    if (this.text[this.position] === "-") {
      buffer.push("-");
      this.position++;
    }

    while (
      this.position < this.text.length &&
      /[0-9.]/.test(this.text[this.position])
    ) {
      buffer.push(this.text[this.position]);
      this.position++;
    }

    return { type: TokenType.Number, lexeme: buffer.join(""), position: start };
  }

  private readIdentifierOrKeyword(): Token {
    const start = this.position;
    const buffer: string[] = [];

    while (this.position < this.text.length) {
      const current = this.text[this.position];
      if (!/[A-Za-z0-9_-]/.test(current)) {
        break;
      }
      buffer.push(current);
      this.position++;
    }

    const lexeme = buffer.join("");
    const normalized = lexeme.toUpperCase();

    switch (normalized) {
      case "AND":
        return { type: TokenType.And, lexeme, position: start };
      case "OR":
        return { type: TokenType.Or, lexeme, position: start };
      case "NOT":
        return { type: TokenType.Not, lexeme, position: start };
      case "IN":
        return { type: TokenType.In, lexeme, position: start };
      case "TRUE":
        return { type: TokenType.Boolean, lexeme: "true", position: start };
      case "FALSE":
        return { type: TokenType.Boolean, lexeme: "false", position: start };
      default:
        return { type: TokenType.Identifier, lexeme, position: start };
    }
  }
}

class Parser {
  private current: Token;
  private tokenizer: Tokenizer;

  constructor(expression: string) {
    this.tokenizer = new Tokenizer(expression);
    this.current = this.tokenizer.nextToken();
  }

  parseExpression(): RuleNode {
    return this.parseOr();
  }

  ensureEnd() {
    if (this.current.type !== TokenType.End) {
      throw new Error(
        `Unexpected token '${this.current.lexeme}' at position ${this.current.position}.`,
      );
    }
  }

  private parseOr(): RuleNode {
    let node = this.parseAnd();
    while (this.match(TokenType.Or)) {
      const right = this.parseAnd();
      const left = node;
      node = {
        evaluate: (answers) =>
          left.evaluate(answers) || right.evaluate(answers),
        collectIds: (ids) => {
          left.collectIds(ids);
          right.collectIds(ids);
        },
      };
    }
    return node;
  }

  private parseAnd(): RuleNode {
    let node = this.parseUnary();
    while (this.match(TokenType.And)) {
      const right = this.parseUnary();
      const left = node;
      node = {
        evaluate: (answers) =>
          left.evaluate(answers) && right.evaluate(answers),
        collectIds: (ids) => {
          left.collectIds(ids);
          right.collectIds(ids);
        },
      };
    }
    return node;
  }

  private parseUnary(): RuleNode {
    if (this.match(TokenType.Not)) {
      const operand = this.parseUnary();
      return {
        evaluate: (answers) => !operand.evaluate(answers),
        collectIds: (ids) => operand.collectIds(ids),
      };
    }

    return this.parsePrimary();
  }

  private parsePrimary(): RuleNode {
    if (this.match(TokenType.LeftParen)) {
      const node = this.parseExpression();
      this.consume(TokenType.RightParen, "Expected ')' after expression.");
      return node;
    }

    return this.parseComparison();
  }

  private parseComparison(): RuleNode {
    const identifier = this.consume(
      TokenType.Identifier,
      "Expected question identifier.",
    );

    const questionId = identifier.lexeme;

    if (this.match(TokenType.Not)) {
      if (this.match(TokenType.In)) {
        const values = this.parseList();
        return this.buildComparisonNode(questionId, "notIn", values);
      }
      throw new Error("Expected IN after NOT for membership comparison.");
    }

    if (this.match(TokenType.In)) {
      const values = this.parseList();
      return this.buildComparisonNode(questionId, "in", values);
    }

    if (this.match(TokenType.Equals)) {
      return this.buildComparisonNode(questionId, "equals", [
        this.parseLiteral(),
      ]);
    }
    if (this.match(TokenType.NotEquals)) {
      return this.buildComparisonNode(questionId, "notEquals", [
        this.parseLiteral(),
      ]);
    }
    if (this.match(TokenType.GreaterOrEqual)) {
      return this.buildComparisonNode(questionId, "gte", [this.parseLiteral()]);
    }
    if (this.match(TokenType.Greater)) {
      return this.buildComparisonNode(questionId, "gt", [this.parseLiteral()]);
    }
    if (this.match(TokenType.LessOrEqual)) {
      return this.buildComparisonNode(questionId, "lte", [this.parseLiteral()]);
    }
    if (this.match(TokenType.Less)) {
      return this.buildComparisonNode(questionId, "lt", [this.parseLiteral()]);
    }

    throw new Error(
      `Expected comparison operator after '${identifier.lexeme}'.`,
    );
  }

  private parseList(): RuleValue[] {
    this.consume(TokenType.LeftBracket, "Expected '[' to start list.");
    const values: RuleValue[] = [this.parseLiteral()];

    while (this.match(TokenType.Comma)) {
      values.push(this.parseLiteral());
    }

    this.consume(TokenType.RightBracket, "Expected ']' after list.");
    return values;
  }

  private parseLiteral(): RuleValue {
    if (this.match(TokenType.String)) {
      return { type: "string", value: this.lastToken.lexeme };
    }

    if (this.match(TokenType.Number)) {
      const parsed = Number(this.lastToken.lexeme);
      if (Number.isNaN(parsed)) {
        throw new Error(`Invalid numeric literal '${this.lastToken.lexeme}'.`);
      }
      return { type: "number", value: parsed };
    }

    if (this.match(TokenType.Boolean)) {
      return {
        type: "boolean",
        value: this.lastToken.lexeme.toLowerCase() === "true",
      };
    }

    throw new Error("Expected literal value.");
  }

  private lastToken: Token = {
    type: TokenType.End,
    lexeme: "",
    position: 0,
  };

  private match(type: TokenType): boolean {
    if (this.current.type === type) {
      this.lastToken = this.current;
      this.current = this.tokenizer.nextToken();
      return true;
    }
    return false;
  }

  private consume(type: TokenType, message: string): Token {
    if (this.current.type === type) {
      const token = this.current;
      this.current = this.tokenizer.nextToken();
      return token;
    }

    throw new Error(message);
  }

  private buildComparisonNode(
    questionId: string,
    operator:
      | "equals"
      | "notEquals"
      | "gt"
      | "gte"
      | "lt"
      | "lte"
      | "in"
      | "notIn",
    values: RuleValue[],
  ): RuleNode {
    return {
      evaluate: (answers) => {
        const answer = answers[questionId];
        if (answer === undefined || answer === null || answer === "") {
          return false;
        }

        switch (operator) {
          case "equals":
            return values[0] ? matches(values[0], answer) : false;
          case "notEquals":
            return values[0] ? !matches(values[0], answer) : false;
          case "gt":
            return compareNumeric(
              values[0],
              answer,
              (left, right) => left > right,
            );
          case "gte":
            return compareNumeric(
              values[0],
              answer,
              (left, right) => left >= right,
            );
          case "lt":
            return compareNumeric(
              values[0],
              answer,
              (left, right) => left < right,
            );
          case "lte":
            return compareNumeric(
              values[0],
              answer,
              (left, right) => left <= right,
            );
          case "in":
            return values.some((value) => matches(value, answer));
          case "notIn":
            return values.every((value) => !matches(value, answer));
          default:
            return false;
        }
      },
      collectIds: (ids) => {
        ids.add(questionId);
      },
    };
  }
}

function matches(value: RuleValue, answer: AnswerValue): boolean {
  if (answer === undefined || answer === null) return false;

  switch (value.type) {
    case "number": {
      const numericAnswer =
        typeof answer === "number" ? answer : Number(answer);
      return !Number.isNaN(numericAnswer) && numericAnswer === value.value;
    }
    case "boolean": {
      const boolAnswer =
        typeof answer === "boolean"
          ? answer
          : `${answer}`.toLowerCase() === "true";
      return boolAnswer === value.value;
    }
    default:
      return `${answer}`.toLowerCase() === value.value.toLowerCase();
  }
}

function compareNumeric(
  value: RuleValue | undefined,
  answer: AnswerValue,
  compare: (left: number, right: number) => boolean,
): boolean {
  if (!value || value.type !== "number") return false;

  const numericAnswer = typeof answer === "number" ? answer : Number(answer);
  if (Number.isNaN(numericAnswer)) return false;

  return compare(numericAnswer, value.value);
}
