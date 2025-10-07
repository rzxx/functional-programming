import "./components/CalcButton";

type Operator = "add" | "subtract" | "multiply" | "divide" | "power";

interface CalculatorState {
  readonly displayValue: string;
  readonly firstOperand: number | null;
  readonly operator: Operator | null;
  readonly waitingForSecondOperand: boolean;
  readonly lastOperator: Operator | null;
  readonly lastOperand: number | null;
  readonly secondaryDisplay: string;
}

const initialState: CalculatorState = {
  displayValue: "0",
  firstOperand: null,
  operator: null,
  waitingForSecondOperand: false,
  lastOperator: null,
  lastOperand: null,
  secondaryDisplay: "",
};

const MAX_DISPLAY_LENGTH = 14;

const limitDisplayLength = (value: string): string => {
  if (value.length <= MAX_DISPLAY_LENGTH) return value;
  return value.slice(0, MAX_DISPLAY_LENGTH);
};

// ✅ Чистые математические функции
const add = (a: number, b: number): number => a + b;
const subtract = (a: number, b: number): number => a - b;
const multiply = (a: number, b: number): number => a * b;
const divide = (a: number, b: number): number => {
  if (b === 0) throw new Error("Division by zero");
  return a / b;
};
const power = (a: number, b: number): number => Math.pow(a, b);
const sqrt = (a: number): number => {
  if (a < 0)
    throw new Error("Cannot calculate square root of a negative number");
  return Math.sqrt(a);
};

const calculate = (a: number, b: number, operator: Operator): number => {
  switch (operator) {
    case "add":
      return add(a, b);
    case "subtract":
      return subtract(a, b);
    case "multiply":
      return multiply(a, b);
    case "divide":
      return divide(a, b);
    case "power":
      return power(a, b);
    default:
      throw new Error("Unknown operator");
  }
};

// ✅ Исправляем ошибки плавающей точки
const formatNumber = (num: number): string => {
  const rounded = parseFloat(num.toFixed(12));
  return rounded.toString();
};

const getOperatorSymbol = (op: Operator): string => {
  switch (op) {
    case "add":
      return "+";
    case "subtract":
      return "−";
    case "multiply":
      return "×";
    case "divide":
      return "÷";
    case "power":
      return "^";
    default:
      return "";
  }
};

// ✅ Обработка ввода числа
const handleNumberInput = (
  state: CalculatorState,
  value: string
): CalculatorState => {
  if (state.waitingForSecondOperand) {
    return {
      ...state,
      displayValue: value,
      waitingForSecondOperand: false,
    };
  }

  if (value === "." && state.displayValue.includes(".")) {
    return state;
  }

  const newDisplayValue =
    state.displayValue === "0" && value !== "."
      ? value
      : state.displayValue + value;

  return { ...state, displayValue: newDisplayValue };
};

// ✅ Обработка действия
const handleAction = (
  state: CalculatorState,
  action: string
): CalculatorState => {
  switch (action) {
    case "clear":
      return initialState;

    case "sqrt": {
      const operand = parseFloat(state.displayValue);
      const result = Math.sqrt(operand);
      return {
        ...initialState,
        displayValue: formatNumber(result),
        secondaryDisplay: `√(${formatNumber(operand)})`, // 🆕
      };
    }

    case "add":
    case "subtract":
    case "multiply":
    case "divide":
    case "power": {
      const inputValue = parseFloat(state.displayValue);

      // Если уже есть firstOperand и оператор, то сначала вычисляем
      if (
        state.firstOperand !== null &&
        state.operator &&
        !state.waitingForSecondOperand
      ) {
        const result = calculate(
          state.firstOperand,
          inputValue,
          state.operator
        );
        const newState: CalculatorState = {
          ...state,
          displayValue: formatNumber(result),
          firstOperand: result,
          operator: action as Operator,
          waitingForSecondOperand: true,
          lastOperand: null,
          lastOperator: null,
        };
        return {
          ...newState,
          secondaryDisplay: updateSecondaryDisplay(newState),
        };
      }

      const newState: CalculatorState = {
        ...state,
        firstOperand: inputValue,
        operator: action as Operator,
        waitingForSecondOperand: true,
      };

      return {
        ...newState,
        secondaryDisplay: updateSecondaryDisplay(newState),
      };
    }

    case "calculate": {
      const inputValue = parseFloat(state.displayValue);

      // повторное "="
      if (
        state.lastOperator &&
        state.lastOperand !== null &&
        state.firstOperand === null
      ) {
        const result = calculate(
          inputValue,
          state.lastOperand,
          state.lastOperator
        );
        const newState: CalculatorState = {
          ...state,
          displayValue: formatNumber(result),
        };
        return {
          ...newState,
          secondaryDisplay: `${formatNumber(inputValue)} ${getOperatorSymbol(
            state.lastOperator
          )} ${formatNumber(state.lastOperand)} =`,
        };
      }

      if (state.firstOperand !== null && state.operator) {
        const result = calculate(
          state.firstOperand,
          inputValue,
          state.operator
        );
        const newState: CalculatorState = {
          ...initialState,
          displayValue: formatNumber(result),
          lastOperator: state.operator,
          lastOperand: inputValue,
          secondaryDisplay: `${formatNumber(
            state.firstOperand
          )} ${getOperatorSymbol(state.operator)} ${formatNumber(
            inputValue
          )} =`, // 🆕
        };
        return newState;
      }

      return state;
    }

    default:
      return state;
  }
};

// ✅ Главная функция обработки кнопки
const handleButtonClick = (
  state: CalculatorState,
  button: HTMLButtonElement
): CalculatorState => {
  const { action, value } = button.dataset;
  try {
    if (value) return handleNumberInput(state, value);
    if (action) return handleAction(state, action);
    return state;
  } catch (error) {
    console.error(error);
    return {
      ...initialState,
      displayValue: "Error",
      secondaryDisplay: "\u00A0", // очищаем secondary display при ошибке
    };
  }
};

// ✅ Поддержка клавиатуры
const mapKeyToAction = (
  key: string
): { value?: string; action?: string } | null => {
  if (/^[0-9.]$/.test(key)) return { value: key };

  switch (key) {
    case "+":
      return { action: "add" };
    case "-":
      return { action: "subtract" };
    case "*":
      return { action: "multiply" };
    case "/":
      return { action: "divide" };
    case "^":
      return { action: "power" };
    case "Enter":
    case "=":
      return { action: "calculate" };
    case "Escape":
      return { action: "clear" };
    case "Backspace":
      return { action: "backspace" }; // можно расширить при желании
    default:
      return null;
  }
};

const updateSecondaryDisplay = (state: CalculatorState): string => {
  const {
    firstOperand,
    operator,
    lastOperator,
    lastOperand,
    displayValue,
    waitingForSecondOperand,
  } = state;

  // Если sqrt был выполнен, это будет задаваться в handleAction

  // После нажатия оператора
  if (firstOperand !== null && operator && waitingForSecondOperand) {
    return `${formatNumber(firstOperand)} ${getOperatorSymbol(operator)}`;
  }

  // После нажатия "="
  if (
    firstOperand !== null &&
    operator === null &&
    lastOperator &&
    lastOperand !== null
  ) {
    return `${formatNumber(firstOperand)} ${getOperatorSymbol(
      lastOperator
    )} ${formatNumber(lastOperand)} =`;
  }

  return "";
};

// ✅ Инициализация
document.addEventListener("DOMContentLoaded", () => {
  const display = document.getElementById("display") as HTMLDivElement;
  const secondaryDisplay = document.getElementById(
    "secondary-display"
  ) as HTMLDivElement;
  const buttons = document.getElementById("button-container") as HTMLDivElement;
  let currentState: CalculatorState = initialState;

  const updateUI = (state: CalculatorState) => {
    display.textContent = limitDisplayLength(state.displayValue);
    secondaryDisplay.textContent = state.secondaryDisplay || "\u00A0"; // ← неразрывный пробел

    // 🆕 Обновляем data-error
    const isError = state.displayValue === "Error";
    display.setAttribute("data-error", String(isError));
  };

  buttons.addEventListener("click", (event) => {
    const target = event.target as HTMLButtonElement;
    if (target.matches("button")) {
      currentState = handleButtonClick(currentState, target);
      updateUI(currentState);
    }
  });

  document.addEventListener("keydown", (e) => {
    const map = mapKeyToAction(e.key);
    if (!map) return;
    if (map.value) {
      currentState = handleNumberInput(currentState, map.value);
    } else if (map.action === "backspace") {
      if (!currentState.waitingForSecondOperand) {
        const newValue =
          currentState.displayValue.length > 1
            ? currentState.displayValue.slice(0, -1)
            : "0";
        currentState = { ...currentState, displayValue: newValue };
      }
    } else if (map.action) {
      currentState = handleAction(currentState, map.action);
    }
    updateUI(currentState);
  });

  updateUI(currentState);
});
