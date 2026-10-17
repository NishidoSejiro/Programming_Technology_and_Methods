using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Marketplace
{
	// =====================================================================
	//  №1. БАНК: абстрактный класс + сберегательный и расчётный счёт
	// =====================================================================

	public abstract class BankAccount
	{
		private static int nextNumber = 1001;   // общий счётчик номеров счетов

		protected decimal balance;
		private readonly List<string> history = new List<string>();

		public int Number { get; }
		public string Owner { get; }
		public decimal Balance { get { return balance; } }
		public List<string> History { get { return history; } }

		// Каждый наследник сообщает, как называется его тип счёта
		public abstract string TypeName { get; }

		protected BankAccount(string owner, decimal initialBalance)
		{
			if (string.IsNullOrWhiteSpace(owner))
			{
				throw new ArgumentException("Имя владельца не может быть пустым.");
			}
			if (initialBalance < 0)
			{
				throw new ArgumentException("Начальный баланс не может быть отрицательным.");
			}

			Owner = owner.Trim();
			balance = initialBalance;
			Number = nextNumber++;
			AddHistory("Открыт счёт. Баланс: " + Money(balance));
		}

		public static string Money(decimal value)
		{
			return value.ToString("N2") + " сом";
		}

		protected void AddHistory(string text)
		{
			history.Add(DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss") + "  " + text);
		}

		// Обычный метод: пополнение одинаково для всех счетов
		public bool Deposit(decimal amount)
		{
			if (amount <= 0)
			{
				Console.WriteLine("[Ошибка] Сумма пополнения должна быть больше 0.");
				return false;
			}

			balance += amount;
			AddHistory("Пополнение +" + Money(amount) + ". Баланс: " + Money(balance));
			return true;
		}

		// Абстрактный метод: у каждого типа счёта свои правила снятия
		// true - снятие прошло, false - отклонено
		public abstract bool Withdraw(decimal amount);

		public override string ToString()
		{
			return "Счёт №" + Number + " (" + TypeName + "), владелец: " + Owner + ", баланс: " + Money(balance);
		}
	}

	public class SavingsAccount : BankAccount
	{
		private readonly decimal minBalance;

		public override string TypeName { get { return "Сберегательный"; } }

		public SavingsAccount(string owner, decimal initialBalance, decimal minBalance)
			: base(owner, CheckInitialBalance(initialBalance, minBalance))
		{
			this.minBalance = minBalance;
		}

		// Проверка выполняется ДО конструктора базового класса,
		// чтобы неудачная попытка не "съедала" номер счёта
		private static decimal CheckInitialBalance(decimal initialBalance, decimal minBalance)
		{
			if (initialBalance >= 0 && initialBalance < minBalance)
			{
				throw new ArgumentException("Начальный баланс сберегательного счёта не может быть меньше " + Money(minBalance) + ".");
			}
			return initialBalance;
		}

		public override bool Withdraw(decimal amount)
		{
			if (amount <= 0)
			{
				Console.WriteLine("[Ошибка] Сумма снятия должна быть больше 0.");
				return false;
			}

			// Проверка минимального остатка
			if (balance - amount < minBalance)
			{
				Console.WriteLine("[Отклонено] На сберегательном счёте должен оставаться минимум " + Money(minBalance) + ". Текущий баланс: " + Money(balance));
				return false;
			}

			balance -= amount;
			AddHistory("Снятие -" + Money(amount) + ". Баланс: " + Money(balance));
			return true;
		}
	}

	public class CheckingAccount : BankAccount
	{
		private readonly decimal serviceFee;

		public override string TypeName { get { return "Расчётный"; } }

		public CheckingAccount(string owner, decimal initialBalance, decimal serviceFee)
			: base(owner, initialBalance)
		{
			this.serviceFee = serviceFee;
		}

		public override bool Withdraw(decimal amount)
		{
			if (amount <= 0)
			{
				Console.WriteLine("[Ошибка] Сумма снятия должна быть больше 0.");
				return false;
			}

			// За каждое снятие банк берёт плату за обслуживание
			decimal totalDeduction = amount + serviceFee;

			if (balance < totalDeduction)
			{
				Console.WriteLine("[Отклонено] Недостаточно средств: нужно " + Money(totalDeduction) + " (с комиссией банка " + Money(serviceFee) + "). Текущий баланс: " + Money(balance));
				return false;
			}

			balance -= totalDeduction;
			AddHistory("Снятие -" + Money(amount) + " (комиссия " + Money(serviceFee) + "). Баланс: " + Money(balance));
			return true;
		}
	}

	// =====================================================================
	//  №2. ОПЛАТА: интерфейс + три способа оплаты
	//  Каждый способ списывает деньги со счёта покупателя (из №1)
	// =====================================================================

	public interface IPaymentMethod
	{
		bool Pay(decimal amount);
	}

	public class CreditCard : IPaymentMethod
	{
		private BankAccount account;

		public CreditCard(BankAccount account)
		{
			this.account = account;
		}

		public bool Pay(decimal amount)
		{
			Console.WriteLine("[Банковская карта] Оплата " + BankAccount.Money(amount) + " картой...");
			return account.Withdraw(amount);
		}
	}

	public class PayPal : IPaymentMethod
	{
		private BankAccount account;

		public PayPal(BankAccount account)
		{
			this.account = account;
		}

		public bool Pay(decimal amount)
		{
			Console.WriteLine("[PayPal] Оплата " + BankAccount.Money(amount) + " через PayPal...");
			return account.Withdraw(amount);
		}
	}

	public class BankTransfer : IPaymentMethod
	{
		private BankAccount account;

		public BankTransfer(BankAccount account)
		{
			this.account = account;
		}

		public bool Pay(decimal amount)
		{
			Console.WriteLine("[Банковский перевод] Оплата " + BankAccount.Money(amount) + " банковским переводом...");
			return account.Withdraw(amount);
		}
	}

	// =====================================================================
	//  МОДЕЛИ ТОВАРА И ЗАКАЗА
	// =====================================================================

	public class Product
	{
		public string name;
		public decimal price;
		public int stock;      // сколько штук на складе

		public Product(string name, decimal price, int stock)
		{
			this.name = name;
			this.price = price;
			this.stock = stock;
		}
	}

	public class OrderItem
	{
		public Product product;
		public int quantity;

		public OrderItem(Product product, int quantity)
		{
			this.product = product;
			this.quantity = quantity;
		}

		public decimal Total()
		{
			return product.price * quantity;
		}
	}

	// =====================================================================
	//  ПРОГРАММА
	// =====================================================================

	class Program
	{
		// Параметры банка и файлов (можно менять)
		const decimal SavingsMinBalance = 1000m;   // минимальный остаток на сберегательном счёте
		const decimal CheckingFee = 50m;           // комиссия за снятие с расчётного счёта
		const string LogFile = "log.txt";          // №4: журнал событий
		const string ReportFile = "report.txt";    // №5: отчёт о продажах

		static List<Product> products = new List<Product>();
		static List<OrderItem> cart = new List<OrderItem>();
		static List<string> sales = new List<string>();   // записи о проданных товарах
		static decimal totalRevenue = 0m;

		static BankAccount buyerAccount;    // расчётный счёт покупателя
		static BankAccount sellerAccount;   // сберегательный счёт магазина
		static string userName = "";

		static string[] mainPageInfo = new string[0];   // данные главной страницы (№7)

		// async Task Main - чтобы можно было использовать await прямо в Main
		static async Task Main(string[] args)
		{
			Console.OutputEncoding = Encoding.UTF8;
			Console.InputEncoding = Encoding.UTF8;

			Console.WriteLine("=== МАРКЕТПЛЕЙС ===");
			WriteEvent("Программа запущена");

			// №7: при запуске параллельно обновляем данные главной страницы
			await LoadMainPageAsync();
			Console.WriteLine();
			Console.WriteLine("Нажмите Enter, чтобы войти в систему...");
			Console.ReadLine();

			// №4 + №1: вход пользователя, открытие его счёта
			Login();
			AddDemoProducts();
			Console.WriteLine();
			Console.WriteLine("Нажмите Enter, чтобы продолжить...");
			Console.ReadLine();

			bool running = true;
			while (running)
			{
				ShowMenu();
				string choice = ReadLine("Ваш выбор: ");
				Console.WriteLine();

				switch (choice)
				{
					case "1": ShowCatalog(); break;
					case "2": AddProduct(); break;
					case "3": AddToCart(); break;
					case "4": PayForCart(); break;
					case "5": ShowMyAccount(); break;
					case "6": await DoReportAsync(); break;
					case "7": ShowLog(); break;
					case "0":
						WriteEvent("Пользователь " + userName + " вышел из системы");
						running = false;
						break;
					default: Console.WriteLine("[Ошибка] Нет такого пункта меню."); break;
				}

				if (running)
				{
					Console.WriteLine();
					Console.WriteLine("Нажмите Enter, чтобы продолжить...");
					Console.ReadLine();
				}
			}

			Console.WriteLine("Спасибо, что выбрали наш маркетплейс! До свидания!");
		}

		static void ShowMenu()
		{
			Console.Clear();
			Console.WriteLine("=== МАРКЕТПЛЕЙС: ГЛАВНОЕ МЕНЮ ===");

			// Данные главной страницы, загруженные при запуске
			foreach (string info in mainPageInfo)
			{
				Console.WriteLine("  " + info);
			}

			Console.WriteLine("Пользователь: " + userName + " | Баланс: " + BankAccount.Money(buyerAccount.Balance));
			Console.WriteLine("---------------------------------");
			Console.WriteLine("1. Каталог товаров");
			Console.WriteLine("2. Добавить товар в каталог (продавец)");
			Console.WriteLine("3. Положить товар в корзину");
			Console.WriteLine("4. Корзина и оплата");
			Console.WriteLine("5. Мой банковский счёт");
			Console.WriteLine("6. Отчёт о продажах (загрузка)");
			Console.WriteLine("7. Журнал событий");
			Console.WriteLine("0. Выход");
			Console.WriteLine("---------------------------------");
		}

		// =====================================================================
		//  №7. Параллельная загрузка данных главной страницы
		// =====================================================================

		static async Task LoadMainPageAsync()
		{
			Console.WriteLine("Обновляем данные главной страницы...");
			Stopwatch timer = Stopwatch.StartNew();

			// Запускаем все три загрузки сразу, не дожидаясь друг друга
			Task<string> currencyTask = LoadCurrencyAsync();
			Task<string> newsTask = LoadNewsAsync();
			Task<string> weatherTask = LoadWeatherAsync();

			// Ждём, пока завершатся все три
			mainPageInfo = await Task.WhenAll(currencyTask, newsTask, weatherTask);

			timer.Stop();
			Console.WriteLine("Главная страница обновлена за " + (timer.ElapsedMilliseconds / 1000.0).ToString("0.0") + " сек.");
			WriteEvent("Данные главной страницы обновлены");
		}

		static async Task<string> LoadCurrencyAsync()
		{
			await Task.Delay(2000);
			return "Курс: 1 USD = 87,45 сом";
		}

		static async Task<string> LoadNewsAsync()
		{
			await Task.Delay(3000);
			return "Новости: скидки до 50% на электронику!";
		}

		static async Task<string> LoadWeatherAsync()
		{
			await Task.Delay(1000);
			return "Погода: Бишкек, +14 °C, ясно";
		}

		// =====================================================================
		//  №4 + №1. Вход в систему и открытие счёта
		// =====================================================================

		static void Login()
		{
			Console.WriteLine();
			Console.WriteLine("--- Вход в систему ---");

			while (userName == "")
			{
				string name = ReadLine("Ваше имя: ");
				if (name == "")
				{
					Console.WriteLine("[Ошибка] Имя не может быть пустым.");
				}
				else
				{
					userName = name;
				}
			}

			// Открываем расчётный счёт покупателя
			while (buyerAccount == null)
			{
				decimal initial = ReadDecimal("Начальный баланс вашего счёта (сом): ");

				try
				{
					buyerAccount = new CheckingAccount(userName, initial, CheckingFee);
				}
				catch (ArgumentException ex)
				{
					// Конструктор бросает исключение при некорректных данных
					Console.WriteLine("[Ошибка] " + ex.Message);
				}
			}

			// Сберегательный счёт магазина: на него приходят деньги за заказы
			sellerAccount = new SavingsAccount("Магазин", 5000m, SavingsMinBalance);

			Console.WriteLine("[Успех] Открыт " + buyerAccount);

			WriteEvent("Пользователь " + userName + " вошёл в систему. Открыт счёт №" + buyerAccount.Number);

			// После входа читаем и показываем журнал
			ShowLog();
		}

		// =====================================================================
		//  Каталог товаров
		// =====================================================================

		static void AddDemoProducts()
		{
			products.Add(new Product("Наушники", 2500m, 10));
			products.Add(new Product("Клавиатура", 1800m, 5));
			products.Add(new Product("Мышь", 900m, 20));
			products.Add(new Product("Флешка 64 ГБ", 700m, 15));
		}

		static void ShowCatalog()
		{
			Console.WriteLine("--- Каталог товаров ---");

			if (products.Count == 0)
			{
				Console.WriteLine("Каталог пока пуст.");
				return;
			}

			for (int i = 0; i < products.Count; i++)
			{
				Product p = products[i];
				Console.WriteLine((i + 1) + ". " + p.name + " - " + BankAccount.Money(p.price) + " (в наличии: " + p.stock + " шт.)");
			}
		}

		static void AddProduct()
		{
			Console.WriteLine("--- Добавление товара ---");

			string name = ReadLine("Название товара: ");
			if (name == "")
			{
				Console.WriteLine("[Ошибка] Название не может быть пустым.");
				return;
			}

			decimal price = ReadPrice("Цена за штуку (сом): ");
			int stock = ReadQuantity("Количество на складе (шт.): ");

			products.Add(new Product(name, price, stock));
			Console.WriteLine("[Успех] Товар добавлен в каталог.");
			WriteEvent("В каталог добавлен товар: " + name + " (" + price.ToString("N2") + " сом, " + stock + " шт.)");
		}

		// =====================================================================
		//  №3. Корзина: ввод количества с обработкой исключений
		// =====================================================================

		static void AddToCart()
		{
			Console.WriteLine("--- Добавление в корзину ---");

			if (products.Count == 0)
			{
				Console.WriteLine("Каталог пуст, добавлять нечего.");
				return;
			}

			ShowCatalog();

			string input = ReadLine("Номер товара: ");
			int number;

			if (!int.TryParse(input, out number))
			{
				Console.WriteLine("[Ошибка] Номер товара должен быть числом.");
				return;
			}
			if (number < 1 || number > products.Count)
			{
				Console.WriteLine("[Ошибка] Товара с таким номером нет.");
				return;
			}

			Product product = products[number - 1];
			int quantity = ReadQuantity("Количество (шт.): ");

			// Проверяем остаток на складе с учётом того, что уже лежит в корзине
			int alreadyInCart = QuantityInCart(product);
			if (alreadyInCart + quantity > product.stock)
			{
				Console.WriteLine("[Ошибка] На складе только " + product.stock + " шт. (в корзине уже " + alreadyInCart + ").");
				return;
			}

			// Если такой товар уже в корзине, увеличиваем количество
			bool found = false;
			foreach (OrderItem item in cart)
			{
				if (item.product == product)
				{
					item.quantity += quantity;
					found = true;
				}
			}
			if (!found)
			{
				cart.Add(new OrderItem(product, quantity));
			}

			Console.WriteLine("[Успех] В корзину добавлено: " + product.name + " x" + quantity);
			WriteEvent(userName + " добавил в корзину: " + product.name + " x" + quantity);
		}

		static int QuantityInCart(Product product)
		{
			int count = 0;
			foreach (OrderItem item in cart)
			{
				if (item.product == product)
				{
					count += item.quantity;
				}
			}
			return count;
		}

		// =====================================================================
		//  Оплата заказа: №2 (способ оплаты) + №1 (списание и зачисление)
		// =====================================================================

		static void PayForCart()
		{
			Console.WriteLine("--- Корзина ---");

			if (cart.Count == 0)
			{
				Console.WriteLine("Корзина пуста.");
				return;
			}

			decimal total = 0m;
			foreach (OrderItem item in cart)
			{
				Console.WriteLine(item.product.name + " x" + item.quantity + " = " + BankAccount.Money(item.Total()));
				total += item.Total();
			}
			Console.WriteLine("ИТОГО: " + BankAccount.Money(total));
			Console.WriteLine("Ваш баланс: " + BankAccount.Money(buyerAccount.Balance) + " (комиссия банка за списание: " + BankAccount.Money(CheckingFee) + ")");
			Console.WriteLine();

			Console.WriteLine("Способ оплаты:");
			Console.WriteLine("  1. Банковская карта");
			Console.WriteLine("  2. PayPal");
			Console.WriteLine("  3. Банковский перевод");
			Console.WriteLine("  0. Отмена");

			// Переменная типа интерфейса может хранить любой из трёх классов
			IPaymentMethod payment = null;
			string methodName = "";

			while (payment == null)
			{
				string choice = ReadLine("Ваш выбор: ");

				if (choice == "1")
				{
					payment = new CreditCard(buyerAccount);
					methodName = "банковская карта";
				}
				else if (choice == "2")
				{
					payment = new PayPal(buyerAccount);
					methodName = "PayPal";
				}
				else if (choice == "3")
				{
					payment = new BankTransfer(buyerAccount);
					methodName = "банковский перевод";
				}
				else if (choice == "0")
				{
					Console.WriteLine("Оплата отменена.");
					return;
				}
				else
				{
					Console.WriteLine("[Ошибка] Выберите 0, 1, 2 или 3.");
				}
			}

			// Какой именно Pay() сработает, зависит от выбранного способа
			bool paid = payment.Pay(total);

			if (!paid)
			{
				Console.WriteLine("[Оплата не прошла] Заказ остался в корзине.");
				WriteEvent("Оплата заказа на " + total.ToString("N2") + " сом отклонена (" + methodName + ")");
				return;
			}

			// Оплата прошла: деньги идут на счёт магазина
			sellerAccount.Deposit(total);

			// Уменьшаем остатки на складе и записываем продажи для отчёта
			foreach (OrderItem item in cart)
			{
				item.product.stock -= item.quantity;
				sales.Add(DateTime.Now.ToString("dd.MM.yyyy HH:mm") + "  " + item.product.name + " x" + item.quantity + " = " + BankAccount.Money(item.Total()) + " (покупатель: " + userName + ")");
			}
			totalRevenue += total;
			cart.Clear();

			Console.WriteLine("[Успех] Заказ оплачен! Остаток на вашем счёте: " + BankAccount.Money(buyerAccount.Balance));
			WriteEvent(userName + " оплатил заказ на " + total.ToString("N2") + " сом (" + methodName + ")");
		}

		// =====================================================================
		//  №1. Банковский счёт пользователя
		// =====================================================================

		static void ShowMyAccount()
		{
			Console.WriteLine("--- Мой банковский счёт ---");
			Console.WriteLine(buyerAccount);
			Console.WriteLine();
			Console.WriteLine("История операций:");
			foreach (string line in buyerAccount.History)
			{
				Console.WriteLine("  " + line);
			}
			Console.WriteLine();

			if (ReadYesNo("Пополнить счёт? (да/нет): "))
			{
				decimal amount = ReadDecimal("Сумма пополнения: ");
				if (buyerAccount.Deposit(amount))
				{
					Console.WriteLine("[Успех] Счёт пополнен. Баланс: " + BankAccount.Money(buyerAccount.Balance));
					WriteEvent(userName + " пополнил счёт на " + amount.ToString("N2") + " сом");
				}
			}
		}

		// =====================================================================
		//  №5. Асинхронная загрузка отчёта о продажах
		// =====================================================================

		static async Task DoReportAsync()
		{
			Console.WriteLine("--- Отчёт о продажах ---");

			string report = await LoadReportAsync();
			await SaveReportAsync(report, ReportFile);

			Console.WriteLine("Загрузка завершена! Отчёт сохранён в файл " + ReportFile);
			Console.WriteLine();
			Console.WriteLine(report);

			WriteEvent("Отчёт о продажах загружен и сохранён в " + ReportFile);
		}

		// Имитация запроса к серверу
		static async Task<string> LoadReportAsync()
		{
			Console.WriteLine("Отправляем запрос на сервер...");

			await Task.Delay(3000);   // имитация задержки: 3 секунды

			Console.WriteLine("Данные получены.");

			StringBuilder report = new StringBuilder();
			report.AppendLine("ОТЧЁТ О ПРОДАЖАХ от " + DateTime.Now.ToString("dd.MM.yyyy HH:mm"));
			report.AppendLine("------------------------------");

			if (sales.Count == 0)
			{
				report.AppendLine("Продаж пока не было.");
			}
			else
			{
				foreach (string sale in sales)
				{
					report.AppendLine(sale);
				}
			}

			report.AppendLine("------------------------------");
			report.AppendLine("Общая выручка: " + BankAccount.Money(totalRevenue));
			report.AppendLine("Баланс счёта магазина: " + BankAccount.Money(sellerAccount.Balance));

			return report.ToString();
		}

		// Асинхронная запись отчёта в файл
		static async Task SaveReportAsync(string report, string path)
		{
			Console.WriteLine("Сохраняем отчёт в файл...");

			try
			{
				await File.WriteAllTextAsync(path, report, Encoding.UTF8);
			}
			catch (IOException ex)
			{
				Console.WriteLine("[Ошибка] Не удалось сохранить файл: " + ex.Message);
			}
		}

		// =====================================================================
		//  №4. Журнал событий (файл log.txt)
		// =====================================================================

		// Добавляем одну строку в конец файла
		static void WriteEvent(string text)
		{
			try
			{
				string line = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss") + "  " + text;
				File.AppendAllText(LogFile, line + Environment.NewLine, Encoding.UTF8);
			}
			catch (IOException ex)
			{
				Console.WriteLine("[Ошибка] Не удалось записать в журнал: " + ex.Message);
			}
		}

		// Читаем файл и выводим на экран
		static void ShowLog()
		{
			Console.WriteLine();
			Console.WriteLine("--- Журнал событий ---");

			if (!File.Exists(LogFile))
			{
				Console.WriteLine("Журнал пока пуст.");
				return;
			}

			try
			{
				string content = File.ReadAllText(LogFile, Encoding.UTF8);
				Console.WriteLine(content);
			}
			catch (IOException ex)
			{
				Console.WriteLine("[Ошибка] Не удалось прочитать журнал: " + ex.Message);
			}
		}

		// =====================================================================
		//  Вспомогательные методы ввода
		// =====================================================================

		static string ReadLine(string prompt)
		{
			Console.Write(prompt);
			string text = Console.ReadLine();
			if (text == null)
			{
				text = "";
			}
			return text.Trim();
		}

		// Повторяет запрос, пока пользователь не введёт число (для балансов и сумм)
		static decimal ReadDecimal(string prompt)
		{
			while (true)
			{
				string input = ReadLine(prompt);

				// Принимаем и запятую, и точку в качестве разделителя
				decimal value;
				if (decimal.TryParse(input, NumberStyles.Number, CultureInfo.CurrentCulture, out value) ||
					decimal.TryParse(input, NumberStyles.Number, CultureInfo.InvariantCulture, out value))
				{
					return value;
				}

				Console.WriteLine("[Ошибка] Введите число (например, 1500 или 250,50).");
			}
		}

		// Спрашиваем "да/нет", true - да, false - нет
		static bool ReadYesNo(string prompt)
		{
			while (true)
			{
				string input = ReadLine(prompt).ToLower();

				if (input == "да" || input == "д" || input == "yes" || input == "y")
				{
					return true;
				}
				if (input == "нет" || input == "н" || input == "no" || input == "n")
				{
					return false;
				}

				Console.WriteLine("[Ошибка] Ответьте \"да\" или \"нет\".");
			}
		}

		// №3: количество товара, ошибки ловим через try/catch
		static int ReadQuantity(string prompt)
		{
			while (true)
			{
				try
				{
					Console.Write(prompt);
					string input = Console.ReadLine();

					int quantity = int.Parse(input);   // если не число - будет FormatException

					if (quantity <= 0)
					{
						// Сами создаём исключение для неправильных значений
						throw new ArgumentException("Количество должно быть больше 0.");
					}

					return quantity;
				}
				catch (FormatException)
				{
					Console.WriteLine("[Ошибка] Это не целое число. Введите, например, 3.");
				}
				catch (OverflowException)
				{
					Console.WriteLine("[Ошибка] Слишком большое число.");
				}
				catch (ArgumentException ex)
				{
					Console.WriteLine("[Ошибка] " + ex.Message);
				}
			}
		}

		// №3: цена товара, ошибки ловим через try/catch
		static decimal ReadPrice(string prompt)
		{
			while (true)
			{
				try
				{
					Console.Write(prompt);
					string input = Console.ReadLine();

					// Заменяем запятую на точку, чтобы работало и 250,50 и 250.50
					input = input.Replace(',', '.');

					decimal price = decimal.Parse(input, CultureInfo.InvariantCulture);

					if (price <= 0)
					{
						throw new ArgumentException("Цена должна быть больше 0.");
					}

					return price;
				}
				catch (FormatException)
				{
					Console.WriteLine("[Ошибка] Это не число. Введите, например, 250 или 250,50.");
				}
				catch (OverflowException)
				{
					Console.WriteLine("[Ошибка] Слишком большое число.");
				}
				catch (ArgumentException ex)
				{
					Console.WriteLine("[Ошибка] " + ex.Message);
				}
			}
		}
	}
}
