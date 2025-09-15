// 1.
const onlyEvens = (array) => array.filter((i) => i % 2 == 0);

const sqr = (array) => array.map((i) => i ** 2);

const objWithSvo = (array, prop) =>
  array.filter((obj) => obj.hasOwnProperty(prop));

const summa = (array) => array.reduce((sum, i) => (sum += i), 0);

// 2.
const myOwnMap = (array, func) => array.map((i) => func(i));

// 3.
// ----
// 3.1.
const numbers = [1, 2, 3, 4, 5, 6, 7, 8];
const evens = onlyEvens(numbers);
const evensSqr = sqr(evens);
const sumSquaresEvens = summa(evensSqr);

console.log("Массив чисел:", numbers);
console.log("Массив чётких чисел:", evens);
console.log("Массив чётких чисел в квадрате:", evensSqr);
console.log("Сумма квадратов чётких чисел:", sumSquaresEvens);

// 3.2.
const testNabor = [
  { svo1: "1", svo2: 21 },
  { svo1: "2" },
  { svo1: "3", svo2: 2562 },
  { svo1: "4" },
  { svo1: "5", svo2: 1235346 },
  { svo1: "6" },
];

const arrayWithSvo2 = objWithSvo(testNabor, "svo2");
const arrayWithBigSvo2 = arrayWithSvo2.filter((i) => i.svo2 > 500);
const avgBigSvo2 =
  arrayWithBigSvo2.reduce((sum, i) => sum + i.svo2, 0) /
  arrayWithBigSvo2.length;

console.log("Массив объектов:", testNabor);
console.log("Объекты с числами:", arrayWithSvo2);
console.log("Объекты с большими числами:", arrayWithBigSvo2);
console.log(
  "Среднее арифметическое чисел объектов с большими числами:",
  avgBigSvo2
);
