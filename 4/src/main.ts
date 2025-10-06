import "./components/CalcButton";
// Определение типов для состояния калькулятора
type Operator = "add" | "subtract" | "multiply" | "divide" | "power";

interface CalculatorState {
  readonly displayValue: string;
  readonly firstOperand: number | null;
  readonly operator: Operator | null;
  readonly waitingForSecondOperand: boolean;
}

// Начальное состояние калькулятора
const initialState: CalculatorState = {
  displayValue: "0",
  firstOperand: null,
  operator: null,
  waitingForSecondOperand: false,
};

// Чистые функции для математических операций
const add = (a: number, b: number): number => a + b;
const subtract = (a: number, b: number): number => a - b;
const multiply = (a: number, b: number): number => a * b;
const divide = (a: number, b: number): number => {
  if (b === 0) {
    throw new Error("Division by zero");
  }
  return a / b;
};
const power = (a: number, b: number): number => Math.pow(a, b);
const sqrt = (a: number): number => {
  if (a < 0) {
    throw new Error("Cannot calculate square root of a negative number");
  }
  return Math.sqrt(a);
};

// Функция для обработки нажатий кнопок
const handleButtonClick = (
  state: CalculatorState,
  button: HTMLButtonElement,
  updateUI: (value: string) => void
): CalculatorState => {
  const { action, value } = button.dataset;

  const updateDisplay = (newState: CalculatorState): CalculatorState => {
    updateUI(newState.displayValue);
    return newState;
  };

  try {
    if (value) {
      if (state.waitingForSecondOperand) {
        return updateDisplay({
          ...state,
          displayValue: value,
          waitingForSecondOperand: false,
        });
      } else {
        const newDisplayValue =
          state.displayValue === "0" ? value : state.displayValue + value;
        return updateDisplay({ ...state, displayValue: newDisplayValue });
      }
    }

    if (action) {
      switch (action) {
        case "clear":
          return updateDisplay(initialState);
        case "sqrt":
          const operand = parseFloat(state.displayValue);
          const result = sqrt(operand);
          return updateDisplay({
            ...initialState,
            displayValue: String(result),
          });
        case "add":
        case "subtract":
        case "multiply":
        case "divide":
        case "power":
          const firstOperand = parseFloat(state.displayValue);
          return updateDisplay({
            ...state,
            firstOperand: firstOperand,
            operator: action as Operator,
            waitingForSecondOperand: true,
            displayValue: state.displayValue, // Keep the current display value until the next number is entered
          });
        case "calculate":
          if (state.firstOperand !== null && state.operator) {
            const secondOperand = parseFloat(state.displayValue);
            let calculationResult = 0;
            switch (state.operator) {
              case "add":
                calculationResult = add(state.firstOperand, secondOperand);
                break;
              case "subtract":
                calculationResult = subtract(state.firstOperand, secondOperand);
                break;
              case "multiply":
                calculationResult = multiply(state.firstOperand, secondOperand);
                break;
              case "divide":
                calculationResult = divide(state.firstOperand, secondOperand);
                break;
              case "power":
                calculationResult = power(state.firstOperand, secondOperand);
                break;
            }
            return updateDisplay({
              ...initialState,
              displayValue: String(calculationResult),
            });
          }
      }
    }
  } catch (error) {
    if (error instanceof Error) {
      return updateDisplay({ ...initialState, displayValue: "Error" });
    }
  }

  return state;
};

// Инициализация приложения
document.addEventListener("DOMContentLoaded", () => {
  const display = document.getElementById("display") as HTMLDivElement;
  const buttons = document.getElementById("button-container") as HTMLDivElement;

  let currentState: CalculatorState = initialState;

  const updateUI = (value: string) => {
    display.textContent = value;
  };

  buttons.addEventListener("click", (event) => {
    const target = event.target as HTMLButtonElement;
    if (target.matches("button")) {
      currentState = handleButtonClick(currentState, target, updateUI);
    }
  });
});
