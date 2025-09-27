const filterNumbersDivisibleBy = (
  numbers: number[],
  divisor: number
): number[] => {
  return numbers.filter((number) => number % divisor === 0);
};

const joinStringsWithSeparator = (
  items: string[],
  separator: string
): string => {
  return items.join(separator);
};

const sortByProperty = <T, K extends keyof T>(items: T[], property: K): T[] => {
  return [...items].sort((a, b) => {
    const valueA = a[property];
    const valueB = b[property];

    if (valueA < valueB) {
      return -1;
    }
    if (valueA > valueB) {
      return 1;
    }
    return 0;
  });
};

const withLogging = <T extends (...args: any[]) => any>(fn: T): T => {
  return ((...args: Parameters<T>): ReturnType<T> => {
    console.log(`Вызов функции '${fn.name}' с аргументами:`, ...args);
    const result = fn(...args);
    console.log(`Функция '${fn.name}' вернула результат:`, result);
    return result;
  }) as T;
};

const numbers = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10];
const multiplesOf3 = filterNumbersDivisibleBy(numbers, 3);
console.log("Числа, кратные 3:", multiplesOf3);
console.log("Исходный массив чисел не изменился:", numbers);

const words = ["Пиво", "это", "реально", "круто"];
const sentence = joinStringsWithSeparator(words, " ");
console.log("\nПравда:", sentence);

interface people {
  id: number;
  name: string;
  swag: number;
}

const chuvaki: people[] = [
  { id: 3, name: "Канье", swag: 1000 },
  { id: 1, name: "i show speed", swag: 10000000 },
  { id: 2, name: "50 Cent", swag: 50 },
];

const sortedByName = sortByProperty(chuvaki, "name");
const sortedBySwag = sortByProperty(chuvaki, "swag");
console.log("\nТипы, отсортированные по имени:", sortedByName);
console.log("Типы, отсортированные по свегу:", sortedBySwag);
console.log("Исходный массив типов не изменился:", chuvaki);

const sum = (a: number, b: number): number => a + b;
const sumWithLogging = withLogging(sum);

console.log("\n--- Логирование вызова ---");
const result = sumWithLogging(5, 10);
console.log("Итоговый результат:", result);
console.log(
  `\n—————————No functions?————————
⠀⣞⢽⢪⢣⢣⢣⢫⡺⡵⣝⡮⣗⢷⢽⢽⢽⣮⡷⡽⣜⣜⢮⢺⣜⢷⢽⢝⡽⣝
⠸⡸⠜⠕⠕⠁⢁⢇⢏⢽⢺⣪⡳⡝⣎⣏⢯⢞⡿⣟⣷⣳⢯⡷⣽⢽⢯⣳⣫⠇
⠀⠀⢀⢀⢄⢬⢪⡪⡎⣆⡈⠚⠜⠕⠇⠗⠝⢕⢯⢫⣞⣯⣿⣻⡽⣏⢗⣗⠏⠀
⠀⠪⡪⡪⣪⢪⢺⢸⢢⢓⢆⢤⢀⠀⠀⠀⠀⠈⢊⢞⡾⣿⡯⣏⢮⠷⠁⠀⠀
⠀⠀⠀⠈⠊⠆⡃⠕⢕⢇⢇⢇⢇⢇⢏⢎⢎⢆⢄⠀⢑⣽⣿⢝⠲⠉⠀⠀⠀⠀
⠀⠀⠀⠀⠀⡿⠂⠠⠀⡇⢇⠕⢈⣀⠀⠁⠡⠣⡣⡫⣂⣿⠯⢪⠰⠂⠀⠀⠀⠀
⠀⠀⠀⠀⡦⡙⡂⢀⢤⢣⠣⡈⣾⡃⠠⠄⠀⡄⢱⣌⣶⢏⢊⠂⠀⠀⠀⠀⠀⠀
⠀⠀⠀⠀⢝⡲⣜⡮⡏⢎⢌⢂⠙⠢⠐⢀⢘⢵⣽⣿⡿⠁⠁⠀⠀⠀⠀⠀⠀⠀
⠀⠀⠀⠀⠨⣺⡺⡕⡕⡱⡑⡆⡕⡅⡕⡜⡼⢽⡻⠏⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀
⠀⠀⠀⠀⣼⣳⣫⣾⣵⣗⡵⡱⡡⢣⢑⢕⢜⢕⡝⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀
⠀⠀⠀⣴⣿⣾⣿⣿⣿⡿⡽⡑⢌⠪⡢⡣⣣⡟⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀
⠀⠀⠀⡟⡾⣿⢿⢿⢵⣽⣾⣼⣘⢸⢸⣞⡟⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀
⠀⠀⠀⠀⠁⠇⠡⠩⡫⢿⣝⡻⡮⣒⢽⠋⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀
—————————————————————————————`
);
