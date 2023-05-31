' Trouble's Brewing (Final Project)
' 4-28-2023 -> 5-31-2023
' Period 1
' Levy Le

Imports System.Drawing.Drawing2D
Imports System.Drawing.Imaging
Imports System.Drawing.Text
Imports System.IO


Public Class Form1

    Public gameObjects As LinkedList(Of GameObject) ' The main list of game objects (to loop through and tick/render them)
    Public newObjects, deadObjects As LinkedList(Of GameObject) ' Lists that will be used to modify gameObjects after iteration
    ' (as the direct list cannot be modified during iteration)
    Public mouseLock, grabLock, hoverGrab, night As Boolean
    Public day As Integer
    Public ordersDone As Integer ' Secretly kept for scoring
    Public gameOver As Boolean
    Public money As AnimatedValue = New AnimatedValue(1000)
    Public dayTransition As AnimatedValue = New AnimatedValue(0)
    Dim score As Integer = -1
    Dim highScore As Integer
    Dim shopX As AnimatedValue = New AnimatedValue(0)
    Dim shopOpen, titleOpen As Boolean
    Dim dayStart As DateTime

    Public pfc As PrivateFontCollection = New PrivateFontCollection() ' PFC and custom font code derived from:
    ' https://stackoverflow.com/questions/13573916/using-custom-fonts-in-my-winform-labels

    Private Sub Form1_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        pfc.AddFontFile(Application.StartupPath.Replace("bin\Debug", "Resources\GermaniaOne-Regular.ttf")) ' An objectively bad way to import a custom font,
        ' but it works (as long as the project stays unpackaged) and I don't have to touch pointers (the alternative is AddMemoryFont, which is too complex).
        Randomize()
        gameObjects = New LinkedList(Of GameObject)
        newObjects = New LinkedList(Of GameObject)
        deadObjects = New LinkedList(Of GameObject)
        mouseLock = False
        grabLock = False
        gameOver = False
        night = False
        hoverGrab = False
        titleOpen = True
        ordersDone = 0
        day = 1
    End Sub

    Private Sub tick()
        ' Tick simply runs before render, allowing objects to all update before they are rendered (not really used to its fullest extent in this game)
        If mouseLock AndAlso (MouseButtons = MouseButtons.None) Then mouseLock = False
        hoverGrab = False ' Used to prevent cauldron interaction if the player is hovering over a grabbable object (already does, but makes it more clear)
        For Each gameObj In gameObjects
            gameObj.tick()
        Next
    End Sub

    Public Sub gameEnd(g As Graphics)
        ' Game over screen
        If mouseLock AndAlso (MouseButtons = MouseButtons.None) Then mouseLock = False
        Dim menuFont As Font = New Font(pfc.Families(0), 50)
        Dim outline As Pen = New Pen(Color.Black, 5)
        outline.LineJoin = LineJoin.Bevel
        g.DrawImage(My.Resources.GameOver, New Rectangle(Width / 2 - 285.5, 100, 571, 96))
        Dim restartButton As Rectangle = New Rectangle(Width / 2 - 158.5, 650, 317, 86)
        Dim exitButton As Rectangle = New Rectangle(Width / 2 - 158.5, 750, 317, 86)
        ' Scoring
        Dim orderBonus As Integer = ordersDone * 100
        Dim dayBonus As Integer = day * 10
        Dim assetsValue As Integer = 0
        For Each cauldron In gameObjects.OfType(Of Cauldron) ' Liquidating all bought objects into their pure monetary values
            Select Case cauldron.type ' Basically more efficient if else statements
                                      ' https://learn.microsoft.com/en-us/dotnet/visual-basic/language-reference/statements/select-case-statement
                Case 1
                    assetsValue += 500
                Case 2
                    assetsValue += 1000
                Case 3
                    assetsValue += 2000
            End Select
        Next
        For Each cauldron In gameObjects.OfType(Of EditCauldron) ' In case the game ends at night, literally the same code as above
            Select Case cauldron.type
                Case 1
                    assetsValue += 500
                Case 2
                    assetsValue += 1000
                Case 3
                    assetsValue += 2000
            End Select
        Next
        For i = 1 To gameObjects.OfType(Of Order).Count()
            assetsValue += 50 * (2 ^ (i - 1)) ' Summing the exponentially growing price for parchment
        Next
        money.snap()
        outlineText(g, "Balance", pfc.Families(0), 30, Brushes.White, outline, New Point(Width / 2 - 50, 230), StringAlignment.Far)
        outlineText(g, Str(money.getValue()).Trim(), pfc.Families(0), 30, Brushes.White, outline, New Point(Width / 2 + 50, 230), StringAlignment.Near)
        outlineText(g, "Liquidated Assets", pfc.Families(0), 30, Brushes.White, outline, New Point(Width / 2 - 50, 270), StringAlignment.Far)
        outlineText(g, Str(assetsValue).Trim(), pfc.Families(0), 30, Brushes.White, outline, New Point(Width / 2 + 50, 270), StringAlignment.Near)
        outlineText(g, "Orders Completed (" + Str(ordersDone).Trim() + ")", pfc.Families(0), 30, Brushes.White, outline, New Point(Width / 2 - 50, 310),
                    StringAlignment.Far)
        outlineText(g, Str(orderBonus).Trim(), pfc.Families(0), 30, Brushes.White, outline, New Point(Width / 2 + 50, 310), StringAlignment.Near)
        outlineText(g, "Days Passed (" + Str(day).Trim() + ")", pfc.Families(0), 30, Brushes.White, outline, New Point(Width / 2 - 50, 350), StringAlignment.Far)
        outlineText(g, Str(dayBonus).Trim(), pfc.Families(0), 30, Brushes.White, outline, New Point(Width / 2 + 50, 350), StringAlignment.Near)
        If score = -1 Then ' Generates score only if it wasn't already set (as this sub renders each tick)
            score = money.getValue() + assetsValue + orderBonus + dayBonus
            ' High score system
            ' https://learn.microsoft.com/en-us/dotnet/visual-basic/developing-apps/programming/drives-directories-files/how-to-read-from-text-files
            Try
                Dim scorePath As String = Application.StartupPath.Replace("bin\Debug", "Resources\savedscores.txt")
                If Not File.Exists(scorePath) Then
                    ' If no previous high score exists, create the file
                    File.Create(scorePath)
                    My.Computer.FileSystem.WriteAllText(scorePath, Str(score), False)
                    highScore = score
                Else
                    ' Read and compare the high score to the current score
                    highScore = (Val(My.Computer.FileSystem.ReadAllText(scorePath)))
                    If highScore < score Then
                        My.Computer.FileSystem.WriteAllText(scorePath, Str(score), False)
                        highScore = score
                    End If
                End If
            Catch ex As Exception
                outlineText(g, "Couldn't load high scores.", pfc.Families(0), 30, Brushes.Red, outline, New Point(20, Height - 30), StringAlignment.Near)
            End Try
        End If
        outlineText(g, "Total Score", pfc.Families(0), 30, Brushes.White, outline, New Point(Width / 2 - 50, 500), StringAlignment.Far)
        outlineText(g, Str(score).Trim(), pfc.Families(0), 30, Brushes.White, outline, New Point(Width / 2 + 50, 500), StringAlignment.Near)
        outlineText(g, "High Score", pfc.Families(0), 30, Brushes.White, outline, New Point(Width / 2 - 50, 540), StringAlignment.Far)
        outlineText(g, Str(highScore).Trim(), pfc.Families(0), 30, Brushes.White, outline, New Point(Width / 2 + 50, 540), StringAlignment.Near)
        outline.Dispose()
        ' Replay/Exit Buttons
        g.DrawImage(My.Resources.ReplayButton, restartButton)
        g.DrawImage(My.Resources.ExitButton, exitButton)
        If Not mouseLock AndAlso MouseButtons = MouseButtons.Left Then
            If restartButton.Contains(MousePosition) Then
                Application.Restart()
            ElseIf exitButton.Contains(MousePosition) Then
                Close()
            End If
        End If
    End Sub

    Private Sub Form1_Paint(sender As Object, e As PaintEventArgs) Handles Me.Paint
        ' According to https://stackoverflow.com/questions/57497422/how-to-stop-flickering-when-redrawing-ellipses-in-windows-forms,
        ' it is faster to use the Graphics from the PaintEventArgs of the Paint event rather than CreateGraphics(), which removes any flickering when redrawing
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias
        'https://learn.microsoft.com/en-us/dotnet/api/system.drawing.graphics.smoothingmode?view=windowsdesktop-8.0
        ' Makes drawn graphics MUCH cleaner
        If Control.ModifierKeys = Keys.Control Then
            Close()
            Return
        End If
        ' This might sound weird, but I'm 99% sure that the way I'm painting the form through
        ' Invalidate makes the keyDown event not work (as the form is too busy painting itself), but there is a way to see what modifier keys are
        ' pressed at any time, so the Control key is now the close key as I can see if it is pressed in render
        ' https://stackoverflow.com/questions/50794158/how-can-i-detect-the-pressed-key-in-vb-net
        If gameOver Then
            gameEnd(e.Graphics)
            If (MouseButtons = MouseButtons.Left) Then mouseLock = True
            Invalidate() ' You will see the importance of this at the bottom of the sub
            Return
        End If
        tick()
        If titleOpen Then
            ' Title Screen
            Dim buttonRect As Rectangle = New Rectangle(Width / 2 - 158.5, 650, 317, 86)
            e.Graphics.DrawImage(My.Resources.Title, New Rectangle(Width / 2 - 439, 100, 878, 434))
            e.Graphics.DrawImage(My.Resources.PlayButton, buttonRect)
            If Not mouseLock AndAlso buttonRect.Contains(MousePosition) AndAlso MouseButtons = MouseButtons.Left Then
                ' Start Game
                titleOpen = False
                day = 1
                night = False
                dayStart = DateTime.Now
                gameObjects.AddFirst(New Order(Width - 300, 50))
                gameObjects.AddFirst(New Cauldron(Width / 2 - 125, Height / 2))
            End If
        Else
            ' Iterating through specific types (using OfType(Of ...)) so that types of objects are properly layered when drawn (cauldrons then orders then potions
            For Each cauldron In gameObjects.OfType(Of EditCauldron).Reverse()
                cauldron.render(e.Graphics)
            Next
            For Each cauldron In gameObjects.OfType(Of Cauldron).Reverse() ' Reversing these lists when rendering for 2 key reasons:
                cauldron.render(e.Graphics)
            Next
            For Each order In gameObjects.OfType(Of Order).Reverse() ' 1 - So that draggable objects (which are grabbed with priority from the front of the list)
                ' are rendered in front of draggable objects with less priority (towards the back)
                order.render(e.Graphics)
            Next
            For Each potion In gameObjects.OfType(Of Potion).Reverse() ' 2 - moveToFront() adds to the front (an easy change to fix but I'm keeping it like this)
                potion.render(e.Graphics)
            Next
            For Each cauldron In gameObjects.OfType(Of Cauldron).Reverse()
                cauldron.renderUI(e.Graphics)
            Next
            ' Removing no longer needed objects post loop
            For Each deadObj In deadObjects
                gameObjects.Remove(deadObj)
            Next
            deadObjects.Clear()
            ' Adding in new objects post loop
            For Each newObj In newObjects
                gameObjects.AddFirst(newObj)
            Next
            newObjects.Clear()
            ' Money display
            Dim coin As Image = My.Resources.Coin
            Dim outline As Pen = New Pen(Color.Black, 10)
            outline.LineJoin = LineJoin.Bevel
            money.updateValue()
            e.Graphics.DrawImage(coin, New Rectangle(5, Height - coin.Height - 5, coin.Width, coin.Height))
            outlineText(e.Graphics, FormatCurrency(money.getValue(), 0), pfc.Families(0), 50, New SolidBrush(Color.White), outline, New Point(90, Height - 50),
                        StringAlignment.Near)
            If money.getValue() < 100 Then e.Graphics.DrawImage(My.Resources.Warning, New Rectangle(60, Height - 50, 40, 40))
            ' Bankruptcy Warning
            dayTransition.updateValue()
            If night Or Not dayTransition.done Then
                Dim darkBrush As SolidBrush = New SolidBrush(Color.FromArgb(200, 10, 10, 10))
                Dim dimBrush As SolidBrush = New SolidBrush(Color.FromArgb(200, 100, 100, 100))
                Dim lightBrush As SolidBrush = New SolidBrush(Color.FromArgb(255, 200, 200, 200))
                ' Next day button
                Dim nextButton As GraphicsPath = New GraphicsPath()
                nextButton.AddEllipse(New Rectangle(27, 27 + 4.5 * dayTransition.getValue(), 66, 65))
                If nextButton.IsVisible(MousePosition) And night Then ' Essentially .contains(Point) for graphics paths
                    ' https://stackoverflow.com/questions/4816297/how-to-know-if-a-graphicspath-contains-a-point-in-c-sharp
                    e.Graphics.FillPath(dimBrush, nextButton)
                    If Not mouseLock AndAlso MouseButtons = MouseButtons.Left Then
                        ' Return to day
                        If money.getValue() < 100 Or ' Bankruptcy
                            gameObjects.OfType(Of EditCauldron).Count < 1 Or ' No cauldrons
                            gameObjects.OfType(Of Order).Count < 1 _ ' No parchment
                            Then
                            gameOver = True ' End game
                        Else
                            For Each cauldron In gameObjects.OfType(Of EditCauldron)
                                cauldron.transmute()
                            Next
                            shopX.setValue(0)
                            dayTransition.setValue(0)
                            night = False
                            day += 1
                            dayStart = DateTime.Now
                            For Each order In gameObjects.OfType(Of Order)
                                order.reactivate()
                            Next
                        End If
                    End If
                Else
                    e.Graphics.FillPath(darkBrush, nextButton)
                End If
                e.Graphics.DrawImage(My.Resources.NextIcon, New Rectangle(27, 27 + 4.5 * dayTransition.getValue(), 66, 65))
                If money.getValue() < 100 Or ' Bankruptcy warning
                    (gameObjects.OfType(Of EditCauldron).Count < 1 And night) Or ' No cauldrons warning
                    gameObjects.OfType(Of Order).Count < 1 _ ' No parchment warning
                    Then e.Graphics.DrawImage(My.Resources.Warning, New Rectangle(60, 60 + 4.5 * dayTransition.getValue(), 40, 40))
                ' Shop UI
                Dim shopToggle As GraphicsPath = New GraphicsPath()
                Dim shopFont As Font = New Font(pfc.Families(0), 40)
                Dim shopFormat As StringFormat = New StringFormat()
                Dim selectedItem As Integer = -1
                shopFormat.Alignment = StringAlignment.Center
                shopX.updateValue()
                shopToggle.AddPie(New Rectangle(Width + shopX.getValue() + (dayTransition.getValue() * -3), Height / 2 - 50, 100, 100), 90, 180)
                If shopToggle.IsVisible(MousePosition) Then
                    e.Graphics.FillPath(dimBrush, shopToggle)
                    If Not mouseLock AndAlso MouseButtons = MouseButtons.Left Then
                        If shopOpen Then
                            shopX.setValue(0)
                        Else
                            shopX.setValue(-300)
                        End If
                        shopOpen = Not shopOpen
                    End If
                Else
                    e.Graphics.FillPath(darkBrush, shopToggle)
                End If
                e.Graphics.FillRectangle(lightBrush, New Rectangle(Width + 5 + shopX.getValue() + (dayTransition.getValue() * -3), Height / 2 - 5, 40, 10))
                If shopOpen Or Not shopX.done Then
                    For i = 0 To 3
                        Dim button As Rectangle = New Rectangle(Width + 5 + shopX.getValue(), 17.5 + 265 * i, 275, 250)
                        Dim price As Integer
                        If button.Contains(MousePosition) Then
                            e.Graphics.FillRectangle(dimBrush, button)
                            selectedItem = i
                        Else
                            e.Graphics.FillRectangle(darkBrush, button)
                        End If
                        Select Case i
                            Case 0
                                e.Graphics.DrawImage(My.Resources.OrderShop, New Rectangle(Width + 17.5 + shopX.getValue(), 60 + 265 * i, 245, 102))
                                price = 50 * (2 ^ gameObjects.OfType(Of Order).Count())
                                ' Prices of parchment scales exponentially, as it becomes better and better to handle more orders
                            Case 1
                                e.Graphics.DrawImage(My.Resources.Cauldron, New Rectangle(Width + 17.5 + shopX.getValue(), 20 + 265 * i, 250, 200))
                                price = 500
                            Case 2
                                e.Graphics.DrawImage(My.Resources.CopperCauldron, New Rectangle(Width + 17.5 + shopX.getValue(), 20 + 265 * i, 250, 200))
                                price = 1000
                            Case 3
                                e.Graphics.DrawImage(My.Resources.GoldCauldron, New Rectangle(Width + 17.5 + shopX.getValue(), 20 + 265 * i, 250, 200))
                                price = 2000
                        End Select
                        e.Graphics.DrawString(FormatCurrency(price, 0), shopFont, lightBrush, New Point(Width + 145 + shopX.getValue(), 210 + 265 * i), shopFormat)
                    Next
                Else
                    e.Graphics.FillRectangle(lightBrush, New Rectangle(Width + 20 + shopX.getValue() + (dayTransition.getValue() * -3), Height / 2 - 20, 10, 40))
                End If
                If Not selectedItem = -1 And night Then
                    Dim descriptionFont As Font = New Font(pfc.Families(0), 24)
                    shopFormat.Alignment = StringAlignment.Near
                    e.Graphics.FillRectangle(darkBrush, New Rectangle(Width - 510 + shopX.getValue(), 17.5, 500, 300))
                    Dim price As Integer
                    Select Case selectedItem
                        Case 0
                            e.Graphics.DrawString("Order Parchment", shopFont, lightBrush, New Point(Width - 510 + shopX.getValue(), 17.5), shopFormat)
                            e.Graphics.DrawString("Another slot to hold more orders.", descriptionFont, lightBrush, New Point(Width - 500 + shopX.getValue(), 100),
                                                  shopFormat)
                            price = 50 * (2 ^ gameObjects.OfType(Of Order).Count())
                        Case 1
                            e.Graphics.DrawString("Iron Cauldron", shopFont, lightBrush, New Point(Width - 510 + shopX.getValue(), 17.5), shopFormat)
                            e.Graphics.DrawString("A basic cauldron. Tried and true.", descriptionFont, lightBrush, New Point(Width - 500 + shopX.getValue(), 100),
                                                  shopFormat)
                            price = 500
                        Case 2
                            e.Graphics.DrawString("Copper Cauldron", shopFont, lightBrush, New Point(Width - 510 + shopX.getValue(), 17.5), shopFormat)
                            e.Graphics.DrawString("A better, faster cauldron.", descriptionFont, lightBrush, New Point(Width - 500 + shopX.getValue(), 100),
                                                  shopFormat)
                            price = 1000
                        Case 3
                            e.Graphics.DrawString("Gold Cauldron", shopFont, lightBrush, New Point(Width - 510 + shopX.getValue(), 17.5), shopFormat)
                            e.Graphics.DrawString("Fast. Not for the faint of heart.", descriptionFont, lightBrush, New Point(Width - 500 + shopX.getValue(), 100),
                                                  shopFormat)
                            price = 2000
                    End Select
                    If money.getValue() < price Then e.Graphics.DrawString("You cannot afford this!", descriptionFont, lightBrush,
                                                                           New Point(Width - 510 + shopX.getValue(), 280), shopFormat)
                    If Not mouseLock AndAlso MouseButtons = MouseButtons.Left AndAlso money.getValue() >= price Then
                        shopX.setValue(0)
                        shopOpen = False
                        money.addValue(-price)
                        resetDrag()
                        My.Computer.Audio.Play(My.Resources.buy, AudioPlayMode.Background)
                        Select Case selectedItem ' Generating new (bought) objects
                            Case 0
                                newObjects.AddLast(New Order(MousePosition, New Point(-125, -15)))
                            Case 1
                                newObjects.AddLast(New EditCauldron(MousePosition, My.Resources.Cauldron, New Point(-125, -100), 1))
                            Case 2
                                newObjects.AddLast(New EditCauldron(MousePosition, My.Resources.CopperCauldron, New Point(-125, -100), 2))
                            Case 3
                                newObjects.AddLast(New EditCauldron(MousePosition, My.Resources.GoldCauldron, New Point(-125, -100), 3))
                        End Select
                    End If
                End If
                darkBrush.Dispose()
                dimBrush.Dispose()
                lightBrush.Dispose()
            End If
            If night Then ' Numeric day display
                outlineText(e.Graphics, ("Night" + Str(day) + "/14"), pfc.Families(0), 50, New SolidBrush(Color.White), outline, New Point(110, 60),
                            StringAlignment.Near)
            Else
                outlineText(e.Graphics, ("Day" + Str(day) + "/14"), pfc.Families(0), 50, New SolidBrush(Color.White), outline, New Point(110, 60),
                            StringAlignment.Near)
            End If
            outline.Dispose()
            ' Day dial display
            Dim dayDial As Image = My.Resources.DayDial
            Dim circleClip As GraphicsPath = New GraphicsPath()
            circleClip.AddEllipse(New Rectangle(27, 27, 66, 65))
            e.Graphics.DrawImage(dayDial, New Rectangle(10, 10, dayDial.Width, dayDial.Height))
            e.Graphics.SetClip(circleClip) ' Clip so that the sun never leaves the dial
            ' https://learn.microsoft.com/en-us/dotnet/api/system.drawing.graphics.clip?view=windowsdesktop-8.0
            Dim sun As Rectangle = New Rectangle(43, 43 + getDayTime() / 9000 * 5, 33, 33) ' Days last 1.5 minutes (90000 milliseconds), moving the sun down 50 pixels
            Dim sunBrush As SolidBrush = New SolidBrush(Color.FromArgb(240, 201, 61))
            Dim sunPen As Pen = New Pen(Color.FromArgb(223, 177, 58), 5)
            e.Graphics.FillEllipse(sunBrush, sun)
            e.Graphics.DrawEllipse(sunPen, sun)
            sunBrush.Dispose()
            sunPen.Dispose()
            If night Or Not dayTransition.done Then
                Dim moon As Image = My.Resources.Moon
                Dim nightBrush As SolidBrush = New SolidBrush(Color.FromArgb(CInt(dayTransition.getValue() / 20 * 255), 90, 69, 105))
                e.Graphics.FillPath(nightBrush, circleClip)
                e.Graphics.DrawImage(moon, New Rectangle(43, 93 - 50 * dayTransition.getValue() / 20, 33, 33))
            Else
                If getDayTime() > 90000 Then
                    If day = 14 Then
                        ' End game (end of time frame: 2 weeks)
                        gameOver = True
                    Else
                        ' Transition into night
                        night = True
                        shopOpen = False
                        shopX.snapValue(0)
                        dayTransition.setValue(20)
                        My.Computer.Audio.Play(My.Resources.night, AudioPlayMode.Background)
                        For Each obj In gameObjects
                            If obj.GetType() = GetType(Cauldron) Then
                                ' Replaces cauldrons with a movable version of themselves during the night
                                newObjects.AddLast(New EditCauldron(obj))
                                deadObjects.AddLast(obj)
                            ElseIf obj.GetType() = GetType(Order) Then
                                obj.deactivate()
                            Else
                                obj.kill()
                            End If
                        Next
                    End If
                End If
            End If
            e.Graphics.ResetClip()
        End If
        If (MouseButtons = MouseButtons.Left) Then mouseLock = True ' mouseLock exists so that clicked objects don't activate unless the mouse was
        ' explicitly pressed down on top of them, as it locks up if it was pressed somewhere else and moved over the object
        Invalidate() ' Marks the form's area as "invalid" so that it will be redrawn
    End Sub

    ' Below are useful functions used by most other classes (thus consolidated into Form1 for ease of access)

    Public Function getDayTime() As Integer
        ' Returns time elapsed in the day in milliseconds
        Return (DateTime.Now.TimeOfDay - dayStart.TimeOfDay).TotalMilliseconds ' https://stackoverflow.com/questions/8688242/calculating-time-between-two-dates
    End Function

    Public Function concatRecipe(recipe As Integer(,)) As String ' A function to condense the recipe array into a workable object (string),
        ' which is easier to manipulate/compare; used throughout different classes
        Dim output As String = ""
        For stage = 0 To 2
            For ingredient = 0 To 3
                output += Str(recipe(stage, ingredient)) + ","
            Next
        Next
        output = output.Replace(" ", "") ' Remove whitespace (added by Str()?)
        Return output.Substring(0, output.Length - 1) ' Remove the last comma in the output
    End Function

    Public Sub moveToFront(gameObj As GameObject) ' Moves an object to the front of its layer (based on type) by
        ' removing it from the list but reading it to the front
        newObjects.AddLast(gameObj)
        ' Adding to the back of the list so that all objects added are moved to the front in the right order (objects moved last are in front)
        deadObjects.AddLast(gameObj)
    End Sub

    Public Sub resetDrag() ' Iterates through all draggable types and releases them
        For Each obj In gameObjects.OfType(Of Potion)
            If obj.grabbed Then
                obj.grabbed = False
                obj.onRelease()
            End If
        Next
        For Each obj In gameObjects.OfType(Of Order)
            If obj.grabbed Then
                obj.grabbed = False
                obj.onRelease()
            End If
        Next
        For Each obj In gameObjects.OfType(Of EditCauldron)
            If obj.grabbed Then
                obj.grabbed = False
                obj.onRelease()
            End If
        Next
    End Sub

    ' Useful paint functions

    Public Sub outlineText(g As Graphics, text As String, font As FontFamily, fontSize As Single,
                           fill As SolidBrush, outline As Pen, position As Point, alignment As Integer)
        ' Creates outlined text https://stackoverflow.com/questions/40310546/how-to-add-text-outline-to-button-text
        Dim textPath As GraphicsPath = New GraphicsPath()
        Dim strFormat As StringFormat = New StringFormat()
        ' https://learn.microsoft.com/en-us/dotnet/desktop/winforms/advanced/how-to-align-drawn-text?view=netframeworkdesktop-4.8
        strFormat.Alignment = alignment
        strFormat.LineAlignment = StringAlignment.Center
        textPath.AddString(text, font, 0, fontSize, position, strFormat)
        g.DrawPath(outline, textPath)
        g.FillPath(fill, textPath)
    End Sub

    Public Function cleanArc(g As Graphics, brush As Brush, x As Single, y As Single,
                             outer As Integer, inner As Integer, startAngle As Integer, sweepAngle As Integer) As GraphicsPath
        ' Alternative to g.drawArc, but without misplaced endcaps (drawing an arc from 0 to 180 would not result in a flat edges on the semicircle)
        ' https://stackoverflow.com/questions/36096759/how-to-draw-a-circular-progressbar-pie-using-graphicspath-in-winform
        Dim arcPath As GraphicsPath = New GraphicsPath()
        With arcPath
            .AddArc(New Rectangle((x - outer / 2), (y - outer / 2), outer, outer), startAngle, sweepAngle) ' Outer edge of the arc
            .AddArc(New Rectangle((x - inner / 2), (y - inner / 2), inner, inner), startAngle + sweepAngle, -sweepAngle) ' Inner edge of the arc
            .CloseFigure()
        End With
        g.FillPath(brush, arcPath)
        Return arcPath ' Returns the path in case further action is taken (outlined with pen, etc.)
    End Function

End Class

Public Class GameObject ' A basic class to be inherited by the main game objects, containing the fundimental tick() and render() subs

    Friend x, y As Integer
    Friend sprite As Image
    Friend dead As Boolean
    Dim deadY As AnimatedValue = New AnimatedValue() ' Used for the slide out animation when kill() is called

    Public Sub New(x As Integer, y As Integer)
        Me.x = x
        Me.y = y
        dead = False
    End Sub

    Public Overridable Sub tick()
        If dead Then
            deadY.updateValue()
            y = deadY.getValue()
            If deadY.done Then Form1.deadObjects.AddLast(Me)
        End If
    End Sub

    Public Overridable Function getBounds() As Rectangle
        Return New Rectangle(x, y, sprite.Width, sprite.Height)
    End Function

    Public Overridable Sub deactivate() ' Put in here just so that orders can use them without needing to be specifically of the type order
    End Sub

    Public Overridable Sub kill()
        dead = True
        deadY.snapValue(y)
        deadY.setValue(-100 - sprite.Height)
    End Sub

    Public Overridable Sub render(g As Graphics)
        ' Renders the object's sprite
        Try
            g.DrawImage(sprite, getBounds()) ' Using getBounds() to create a rectangle with a defined width and height, as just using a point
            ' for position scaled the image
        Catch ex As Exception
            g.FillRectangle(Brushes.Red, New Rectangle(x, y, 100, 100))
        End Try
    End Sub

End Class

Public Class Draggable ' A class based off of gameObject with additional functionality for draggable objects
    Inherits GameObject

    Friend grabbed, grabPrimed As Boolean
    Friend grabOffset As Point

    Public Sub New(x As Integer, y As Integer)
        MyBase.New(x, y)
        grabbed = False
        grabPrimed = False
    End Sub

    Public Overrides Sub tick()
        MyBase.tick()
        If grabbed Then
            If Form1.MouseButtons = MouseButtons.Left Then
                x = Form1.MousePosition.X + grabOffset.X
                y = Form1.MousePosition.Y + grabOffset.Y
                If x < 0 Then x = 0 ' Restricts draggable objects to form bounds
                If y < 0 Then y = 0
                If x + sprite.Width > Form1.Width Then x = Form1.Width - sprite.Width
                If y + sprite.Height > Form1.Height Then y = Form1.Height - sprite.Height
            Else
                grabbed = False
                Form1.grabLock = False
                onRelease()
            End If
        Else
            If Form1.grabLock Then Return ' Prevents multiple items from being grabbed at once
            If getBounds().Contains(Form1.MousePosition) Then
                Form1.hoverGrab = True
                If Form1.MouseButtons = MouseButtons.Left Then
                    If grabPrimed Then
                        grabbed = True
                        Form1.grabLock = True
                        grabOffset = Point.Subtract(New Point(x, y), Form1.MousePosition) ' Keeps the position of the object relative to the mouse
                        Form1.moveToFront(Me)
                        onGrab()
                    End If
                Else
                    grabPrimed = True
                End If
            Else
                grabPrimed = False
            End If

        End If
    End Sub

    Overridable Sub onGrab() ' Event handlers for grabbing and releasing
    End Sub

    Overridable Sub onRelease()
    End Sub

End Class

Public Class AnimatedValue ' An object that can interpolate between two values for smooth transitions

    Public duration As Single
    Public done As Boolean
    Dim value, originalValue, difference, targetValue As Single
    Dim frame As Integer

    Public Sub New()
        value = 0
        duration = 10
        frame = 0
        done = True
    End Sub

    Public Sub New(startValue As Single)
        Me.New()
        value = startValue
        duration = 10
        frame = 10
        done = True
    End Sub

    Public Function updateValue() As Single
        ' Updates the value, progressing through the transition
        If frame >= duration Then
            done = True
            Return value
        End If
        frame += 1
        value = originalValue + (difference * (1 - (frame / duration - 1) ^ 2)) ' (frame / duration) provides a t from 0 to 1
        ' (the whole progression of the animation), which is then scaled by the difference needed to cover; the function itself
        ' is an upside down parabola from 0 to 1 (which "eases out" of the animation).
        Return value
    End Function

    Public Function getValue() As Single
        Return value
    End Function

    Public Sub setValue(target As Single)
        ' Sets the number to transition toward
        If targetValue = target Then Return ' Do nothing if the value is already moving to the correct value
        done = False
        originalValue = value
        difference = target - value
        targetValue = target
        frame = 0
    End Sub

    Public Sub snapValue(target As Single)
        ' Directly sets the value to a certain number, without transitions
        frame = duration
        value = target
        originalValue = target
        targetValue = target
        done = True
    End Sub

    Public Sub addValue(number As Single)
        ' Adds a value to the number
        If frame < duration Then value = targetValue ' In case the function is spammed added values don't get lost in transition
        done = False
        originalValue = value
        difference = number
        targetValue = value + difference
        frame = 0
    End Sub

    Public Sub snap()
        ' Skips all transitions in case the true value is needed
        frame = duration
        value = targetValue
        originalValue = value
        done = True
    End Sub

End Class

Public Class Potion
    Inherits Draggable

    Dim potionColor() As Single
    Dim recipe(0 To 2, 0 To 3) As Integer
    Dim bonusValue As Integer
    Dim selectedOrder As Order

    Public Sub New(x As Integer, y As Integer, offset As Point, recipe As Integer(,), bonusValue As Integer)
        MyBase.New(x, y)
        grabbed = True
        Form1.grabLock = True
        grabOffset = offset
        sprite = My.Resources.PotionBase
        Me.recipe = recipe
        Me.bonusValue = bonusValue
        selectedOrder = Nothing
        ' "Hashes" the potion color based on its ingredients
        Dim condensedRecipe() As Single = { ' Sums the total amounts of each ingredient (regardless of the stage which they were added)
            recipe(0, 0) + recipe(1, 0) + recipe(2, 0),
            recipe(0, 1) + recipe(1, 1) + recipe(2, 1),
            recipe(0, 2) + recipe(1, 2) + recipe(2, 2),
            recipe(0, 3) + recipe(1, 3) + recipe(2, 3)
        }
        Dim normRecipe() As Single = normalize(condensedRecipe)
        ' Potion color palette derived (with modification) from: https://colorhunt.co/palette/c6d57ed57e7ea2cdcdffe1af
        ' Base colors: #E67E7E (Red: Shroom), #C6E67E (Green: Herb), #E6E67E (Yellow: Eye), #9AB8E6 (Blue: Crystal)
        Dim baseColors()() As Single = {
            (New Single() {230, 126, 126}), ' Red #E67E7E
            (New Single() {198, 230, 126}), ' Green #C6E67E
            (New Single() {230, 230, 126}), ' Yellow #E6E67E
            (New Single() {154, 184, 230})  ' Blue #9AB8E6
        } ' Jagged array for colors: https://learn.microsoft.com/en-us/dotnet/visual-basic/programming-guide/language-features/arrays/#jagged-arrays
        Dim finalColor As Single() = {0, 0, 0}
        For i = 0 To 3
            Dim scaledColor() As Single = colorScale(baseColors(i), normRecipe(i))
            For h = 0 To 2
                finalColor(h) += scaledColor(h)
            Next
        Next
        finalColor = colorScale(normalize(finalColor), 230 / 255)
        potionColor = {finalColor(0), finalColor(1), finalColor(2), 0, 1}
    End Sub

    Public Overrides Sub tick()
        MyBase.tick()
        If grabbed Then
            selectedOrder = Nothing ' https://learn.microsoft.com/en-us/dotnet/visual-basic/language-reference/nothing
            For Each order In Form1.gameObjects.OfType(Of Order)
                order.potionHover = False
                If IsNothing(selectedOrder) AndAlso order.getPaperRect().Contains(Form1.MousePosition) Then
                    selectedOrder = order
                    order.potionHover = True
                    ' Not just exiting the loop here as other orders need to have potionHover reset
                End If
            Next
        End If
    End Sub

    Public Overrides Sub onRelease()
        MyBase.onRelease()
        If New Rectangle(Form1.Width - 160, Form1.Height - 190, 150, 180).Contains(Form1.MousePosition) Then Form1.deadObjects.AddLast(Me) ' Trashing
        If Not IsNothing(selectedOrder) Then
            ' Selling a potion
            If Form1.concatRecipe(recipe) = Form1.concatRecipe(selectedOrder.recipe) Then
                Form1.ordersDone += 1
                Dim ingredientCount = 0
                For stage = 0 To 2
                    For ingredient = 0 To 3
                        ingredientCount += recipe(stage, ingredient)
                    Next
                Next
                Form1.money.addValue(ingredientCount * 20 + bonusValue)
                My.Computer.Audio.Play(My.Resources.money, AudioPlayMode.Background)
            Else
                My.Computer.Audio.Play(My.Resources.fail, AudioPlayMode.Background)
            End If
            selectedOrder.deactivate()
            Form1.deadObjects.AddLast(Me)
        End If
    End Sub

    Private Function colorScale(color() As Single, scalar As Single) As Single()
        ' A function to iterate through an array and multiply all values by a scalar (used for color blending)
        Dim newColor() As Single = color
        For i = 0 To color.Count - 1
            newColor(i) = color(i) * scalar
        Next
        Return newColor
    End Function

    Private Function normalize(base() As Single) As Single()
        ' "Normalizes" an array by dividing all components by its maximum value, allowing for the ratio between
        ' values to remain constant while the actual values are altered
        Dim maxVal As Single = base.Max
        Dim normVals(base.Count - 1) As Single
        For i = 0 To base.Count - 1
            normVals(i) = base(i) / maxVal
        Next
        Return normVals
    End Function

    Public Overrides Sub render(g As Graphics)
        If grabbed Then
            ' Trash Can
            Dim trashRect As Rectangle = New Rectangle(Form1.Width - 160, Form1.Height - 190, 150, 180)
            If trashRect.Contains(Form1.MousePosition) Then
                g.DrawImage(My.Resources.TrashOpen, trashRect)
            Else
                g.DrawImage(My.Resources.TrashClosed, trashRect)
            End If
        End If
        MyBase.render(g)
        ' Color transform code derived from:
        ' https://learn.microsoft.com/en-us/dotnet/desktop/winforms/advanced/how-to-translate-image-colors?view=netframeworkdesktop-4.8
        Dim imgAttr As ImageAttributes = New ImageAttributes ' The ImageAttributes class applies a "filter" to images when drawn
        imgAttr.SetColorMatrix(New ColorMatrix({
                                                New Single() {1, 0, 0, 0, 0}, ' All red values are multiplied by 1 (kept same)  
                                                New Single() {0, 1, 0, 0, 0}, ' All green values are multiplied by 1 (kept same)  
                                                New Single() {0, 0, 1, 0, 0}, ' All blue values are multiplied by 1 (kept same)  
                                                New Single() {0, 0, 0, 1, 0}, ' All alpha values are multiplied by 1 (kept same)  
                                                potionColor}), ColorMatrixFlag.Default, ColorAdjustType.Bitmap) ' Values will be "translated" (added by this)
        ' The potion liquid sprite is black as adding color via transforms will add to the base, allowing for any color to be made by adding the right values
        g.DrawImage(My.Resources.PotionLiquid, getBounds(), 0, 0, sprite.Width, sprite.Height, GraphicsUnit.Point, imgAttr)
        ' Drawing an image with ImageAttributes also needs a "source rectangle" (part of the image that will be displayed) and a GraphicsUnit
        If grabbed Then
            g.TranslateTransform(x + sprite.Width / 2, y + sprite.Height / 2)
            g.RotateTransform(45)
            g.DrawImage(My.Resources.PotionStopper, New Rectangle(-sprite.Width / 2, -sprite.Height / 2, sprite.Width, sprite.Height))
            ' Potion "rotates" on pickup (liquid and gleam are independent of rotation, so they don't move)
            g.ResetTransform()
        Else
            g.DrawImage(My.Resources.PotionStopper, getBounds())
        End If
    End Sub

End Class

Public Class Cauldron
    Inherits GameObject

    Dim brewStart As DateTime
    Dim potionMenuActive As Boolean
    Dim totalTime As Integer = 10000
    Dim brewStage As Integer ' -1 for no brew, 0 for green, 1 for yellow, 2 for red
    Dim fallingIngredients, fallenIngredients As LinkedList(Of Integer())
    Dim recipe(0 To 2, 0 To 3) As Integer
    Dim ingredientHistory As Bitmap
    Dim historyGraphics As Graphics
    Public type As Integer ' 1 for iron, 2 for copper, 3 for gold
    ' Ingredients put into the cauldron will be stored in three separate "arrays" (based on the first index of the recipe array;
    ' one for each "stage" of brewing). The amount of the four hardcoded ingredients will be stored in the second index in the following order (CW):
    ' Shroom, Herb, Eye, Crystal

    Public Sub New()
        MyBase.New(0, 0)
        fallingIngredients = New LinkedList(Of Integer())
        fallenIngredients = New LinkedList(Of Integer()) ' Essentially deadObjects (from Form1) for fallingIngredients
        recipe = {{0, 0, 0, 0}, {0, 0, 0, 0}, {0, 0, 0, 0}} ' Here it may be easier to see how the ingredient amounts are stored,
        ' split into three separate "subarrays" for each of the three brewing stages.
        potionMenuActive = False
        brewStage = -1
    End Sub

    Public Sub New(x As Integer, y As Integer)
        Me.New()
        Me.x = x
        Me.y = y
        sprite = My.Resources.Cauldron
        type = 1
        setupStats()
    End Sub

    Public Sub New(base As EditCauldron)
        Me.New()
        Me.x = base.x
        Me.y = base.y
        Me.sprite = base.sprite
        Me.type = base.type
        setupStats()
    End Sub

    Private Sub setupHistory()
        ' The ingredients history (the semicircle at the bottom) shows the ingredients at the time they were added
        ' This is rendered as a bitmap of the background at first, and as ingredients are added they are added to the bitmap
        ingredientHistory = New Bitmap(650, 350)
        historyGraphics = Graphics.FromImage(ingredientHistory)
        historyGraphics.SmoothingMode = SmoothingMode.AntiAlias
        Dim historyPen As Pen = New Pen(Color.Black, 10)
        historyGraphics.DrawPath(historyPen, Form1.cleanArc(historyGraphics, Brushes.Black, 325, 162.5, 325, 300, -5, 190))
        historyPen.Dispose()
        Form1.cleanArc(historyGraphics, New SolidBrush(Color.FromArgb(117, 200, 100)), 325, 162.5, 325, 300, 120, 65)
        Form1.cleanArc(historyGraphics, New SolidBrush(Color.FromArgb(240, 201, 61)), 325, 162.5, 325, 300, 60, 60)
        Form1.cleanArc(historyGraphics, New SolidBrush(Color.FromArgb(196, 76, 68)), 325, 162.5, 325, 300, -5, 65)
    End Sub

    Private Sub setupStats()
        Select Case type
            Case 1
                ' Iron
                totalTime = 30000
            Case 2
                ' Copper
                totalTime = 20000
            Case 3
                ' Gold
                totalTime = 10000
        End Select
    End Sub

    Private Sub addIngredient(ingredient As Integer)
        If Form1.money.getValue < 10 Then Return ' Check money
        Form1.money.addValue(-10)
        If brewStage = -1 AndAlso recipe(0, 0) + recipe(0, 1) + recipe(0, 2) + recipe(0, 3) = 0 Then
            ' Starts the brewing timer if this is the first ingredient added
            ' Adding up the values of the first four items to effectively check if they all equal 0 (otherwise the sum is not 0).
            brewStart = DateTime.Now
            setupHistory()
            brewStage = 0
        End If
        recipe(brewStage, ingredient) += 1
        Dim brewingAngle As Single = Math.Min(getBrewTime() * 180 / totalTime, 180) * Math.PI / 180 ' Same calculation as for the brew dial (with a lower bound),
        ' but converted to radians
        Select Case ingredient ' Adding the ingredients at their positions (via sin and cos of the brewing angle) on the history to the bitmap 
            Case 0
                historyGraphics.DrawImage(My.Resources.Shroom, New Rectangle(325 + Math.Cos(brewingAngle) * -155 - 15, 150 + Math.Sin(brewingAngle) * 155, 30, 30))
            Case 1
                historyGraphics.DrawImage(My.Resources.Herb, New Rectangle(325 + Math.Cos(brewingAngle) * -155 - 15, 150 + Math.Sin(brewingAngle) * 155, 30, 30))
            Case 2
                historyGraphics.DrawImage(My.Resources.Eye, New Rectangle(325 + Math.Cos(brewingAngle) * -155 - 15, 150 + Math.Sin(brewingAngle) * 155, 30, 30))
            Case 3
                historyGraphics.DrawImage(My.Resources.Crystal, New Rectangle(325 + Math.Cos(brewingAngle) * -155 - 15, 150 + Math.Sin(brewingAngle) * 155, 30, 30))
        End Select
        fallingIngredients.AddLast({ingredient, sprite.Height + 10, 2})
        ' Stored as ingredient type, y offset, and dy (velocity)
        My.Computer.Audio.Play({My.Resources.bubble1, My.Resources.bubble2, My.Resources.bubble3,
                               My.Resources.bubble4, My.Resources.bubble5}(Int(Rnd() * 5)), AudioPlayMode.Background)
        ' Plays one of five different sounds for adding an ingredient
    End Sub

    Private Function getBrewTime() As Integer
        ' Returns the total seconds spent brewing (or cooling down)
        If brewStage = -1 Then Return -1
        Return (DateTime.Now.TimeOfDay - brewStart.TimeOfDay).TotalMilliseconds
    End Function

    Public Overrides Sub tick()
        MyBase.tick()
        If brewStage = -1 Then Return
        Select Case brewStage ' Updating the brew stage
            Case 0
                If getBrewTime() > Math.Round(totalTime / 3) Then
                    brewStage = 1
                    My.Computer.Audio.Play(My.Resources.brewStage, AudioPlayMode.Background)
                End If
            Case 1
                If getBrewTime() > Math.Round(2 * totalTime / 3) Then
                    brewStage = 2
                    My.Computer.Audio.Play(My.Resources.brewStage, AudioPlayMode.Background)
                End If
        End Select
    End Sub

    Public Overrides Sub render(g As Graphics)
        ' Constants used for rendering
        Dim centerX As Integer = x + CInt(sprite.Width / 2)
        Dim centerY As Integer = y + CInt(sprite.Height / 2)
        For Each ingredient In fallingIngredients
            Select Case ingredient(0)
                Case 0
                    g.DrawImage(My.Resources.Shroom, New Rectangle(centerX - 30, centerY - ingredient(1), 60, 60))
                Case 1
                    g.DrawImage(My.Resources.Herb, New Rectangle(centerX - 30, centerY - ingredient(1), 60, 60))
                Case 2
                    g.DrawImage(My.Resources.Eye, New Rectangle(centerX - 30, centerY - ingredient(1), 60, 60))
                Case 3
                    g.DrawImage(My.Resources.Crystal, New Rectangle(centerX - 30, centerY - ingredient(1), 60, 60))
            End Select
            ingredient(1) -= ingredient(2) ' Velocity (dy)
            ingredient(2) += 1 ' Gravity/Acceleration (dy^2)
            If ingredient(1) <= 0 Then fallenIngredients.AddLast(ingredient)
        Next
        For Each ingredient In fallenIngredients
            fallingIngredients.Remove(ingredient)
        Next
        MyBase.render(g)
    End Sub

    Public Sub renderUI(g As Graphics)
        ' Render UI after all cauldrons are rendered so that they are always visible
        Dim centerX As Integer = x + CInt(sprite.Width / 2)
        Dim centerY As Integer = y + CInt(sprite.Height / 2)
        Dim darkBrush As SolidBrush = New SolidBrush(Color.FromArgb(200, 10, 10, 10))
        Dim lightBrush As SolidBrush = New SolidBrush(Color.FromArgb(200, 200, 200, 200))
        ' Rendering the brew stage dial
        If Not brewStage = -1 Then
            Dim dial = My.Resources.BrewDial
            Dim arrow = My.Resources.BrewDialArrow
            g.DrawImage(dial, x + 82, y - 55, 86, 40)
            g.TranslateTransform(centerX, y - 25)
            g.RotateTransform(Math.Min(getBrewTime() * 180 / totalTime, 190) - 90) ' Uses Math.Min to stop the arrow from going past the dial's end
            g.DrawImage(arrow, -CInt(arrow.Width / 2) + 1, -22, arrow.Width, arrow.Height)
            g.ResetTransform()
        End If
        If Form1.grabLock Or Form1.hoverGrab Then Return ' Don't render/handle cauldron interactions if something is grabbed/dragged
        If getBounds().Contains(Form1.MousePosition) Then
            Dim mouseDist = Math.Sqrt((centerX - Form1.MousePosition.X) ^ 2 + (centerY - Form1.MousePosition.Y) ^ 2)
            If Not brewStage = -1 Then g.FillEllipse(darkBrush, centerX - 30, centerY - 30, 60, 60)
            If potionMenuActive Then
                ' Rendering potion bottling event (a arrow bounces back and forth, timing the bottling correctly rewards extra money)
                ' this event will be known as a QTE in the code (quicktime event, probably a misnomer)
                ' The bottom half of the circle (actual button where the player clicks to bottle a potion)
                Form1.cleanArc(g, darkBrush, centerX, centerY + 5, 225, 75, 0, 180)
                ' The top half of the circle (the actual QTE with the arrow) (halves are offset in height to avoid overlap)
                Form1.cleanArc(g, darkBrush, centerX, centerY - 5, 225, 75, -180, 180)
                Form1.cleanArc(g, New SolidBrush(Color.FromArgb(100, 30, 30, 30)), centerX, centerY - 5, 225, 75, -180, 180)
                Form1.cleanArc(g, New SolidBrush(Color.FromArgb(100, 70, 70, 70)), centerX, centerY - 5, 225, 75, -135, 90)
                Form1.cleanArc(g, New SolidBrush(Color.FromArgb(100, 110, 110, 110)), centerX, centerY - 5, 225, 75, -105, 30)
                ' The different arcs represent the different "regions" of the circle, increasingly narrow to the center
                If mouseDist > 40 And mouseDist < 110 Then
                    Dim mouseAngle = Math.Atan2(y + 100 - Form1.MousePosition.Y, x + 125 - Form1.MousePosition.X)
                    If mouseAngle < 0 And mouseAngle > -Math.PI Then
                        Form1.cleanArc(g, lightBrush, centerX, centerY + 5, 225, 75, 0, 180)
                        If Not Form1.mouseLock AndAlso (Form1.MouseButtons = MouseButtons.Left) Then
                            Form1.resetDrag()
                            Form1.newObjects.AddFirst(New Potion(Form1.MousePosition.X - 50, Form1.MousePosition.Y - 50, New Point(-50, -50), recipe,
                                                                 CInt(20 - Math.Abs(90 - CDec(Math.Abs(180 - ((getBrewTime() / 5) Mod 360)))) / 9 * 2)))
                            ' Bonus money is calculated by getting the absolute difference between the current arrow's angle and 90 (the center),
                            ' then putting it between 0 and 20
                            recipe = {{0, 0, 0, 0}, {0, 0, 0, 0}, {0, 0, 0, 0}} ' Resetting the cauldron's state
                            potionMenuActive = False
                            brewStage = -1
                            setupHistory()
                            My.Computer.Audio.Play(My.Resources.bottle, AudioPlayMode.Background)
                        End If
                    End If
                End If
                Form1.cleanArc(g, New SolidBrush(Color.FromArgb(255, 200, 200, 200)), centerX, centerY - 5, 225, 75,
                               -CDec(Math.Abs(180 - ((getBrewTime() / 5) Mod 360))) - 3, 6)
                ' The QTE arrow is rendered as a small arc with a start angle that oscillates from -180 to 0 (by getting the absolute difference between 180
                ' time mod 360 so that it smoothly goes back and forth (0 and 360 will produce the same value, allowing for the transition to be seamless)
            Else
                ' Rendering the ingredient wheel
                Form1.cleanArc(g, darkBrush, centerX, centerY, 225, 75, 0, 360)
                If mouseDist > 40 And mouseDist < 110 Then
                    Dim mouseAngle = Math.Atan2(y + 100 - Form1.MousePosition.Y, x + 125 - Form1.MousePosition.X)
                    Select Case mouseAngle
                        Case -Math.PI / 4 To Math.PI / 4
                            Form1.cleanArc(g, lightBrush, centerX, centerY, 225, 75, 135, 90)
                            If Not Form1.mouseLock AndAlso (Form1.MouseButtons = MouseButtons.Left) Then addIngredient(3)
                        Case Math.PI / 4 To 3 * Math.PI / 4
                            Form1.cleanArc(g, lightBrush, centerX, centerY, 225, 75, 225, 90)
                            If Not Form1.mouseLock AndAlso (Form1.MouseButtons = MouseButtons.Left) Then addIngredient(0)
                        Case -3 * Math.PI / 4 To -Math.PI / 4
                            Form1.cleanArc(g, lightBrush, centerX, centerY, 225, 75, 45, 90)
                            If Not Form1.mouseLock AndAlso (Form1.MouseButtons = MouseButtons.Left) Then addIngredient(2)
                        Case Else
                            Form1.cleanArc(g, lightBrush, centerX, centerY, 225, 75, -45, 90)
                            If Not Form1.mouseLock AndAlso (Form1.MouseButtons = MouseButtons.Left) Then addIngredient(1)
                    End Select
                End If
                g.DrawImage(My.Resources.Shroom, New Rectangle(centerX - 30, centerY - 105, 60, 60))
                g.DrawImage(My.Resources.Herb, New Rectangle(centerX + 45, centerY - 30, 60, 60))
                g.DrawImage(My.Resources.Eye, New Rectangle(centerX - 30, centerY + 45, 60, 60))
                g.DrawImage(My.Resources.Crystal, New Rectangle(centerX - 105, centerY - 30, 60, 60))
            End If
            If Not brewStage = -1 Then
                ' Rendering the ingredient history
                g.DrawImage(ingredientHistory, New Rectangle(centerX - 325, centerY - 175, 650, 350))
                If potionMenuActive Then
                    g.DrawImage(My.Resources.CancelIcon, New Rectangle(centerX - 29, centerY - 29, 60, 60))
                    g.DrawImage(My.Resources.PotionIcon, New Rectangle(centerX - 29, centerY + 46, 60, 60))
                Else
                    g.DrawImage(My.Resources.PotionIcon, New Rectangle(centerX - 29, centerY - 29, 60, 60))
                End If
                If mouseDist < 30 Then
                    g.FillEllipse(lightBrush, centerX - 30, centerY - 30, 60, 60)
                    If Not Form1.mouseLock AndAlso (Form1.MouseButtons = MouseButtons.Left) Then potionMenuActive = Not potionMenuActive
                End If
            End If
        End If
        darkBrush.Dispose()
        lightBrush.Dispose()
    End Sub

End Class

Public Class EditCauldron
    Inherits Draggable

    Dim safePosition As Point = New Point(-404, 0) ' Arbitrarily -404 to check if a safe position exists or not
    Dim isNew As Boolean ' Used to check if a cauldron was just purchased this night or carried over from the day ("used")
    Public type As Integer ' 0 for stone, 1 for iron, 2 for copper, 3 for gold

    Public Sub New(base As Cauldron)
        MyBase.New(base.x, base.y)
        Me.type = base.type
        sprite = base.sprite
        safePosition = New Point(x, y)
        isNew = False
    End Sub

    Public Sub New(position As Point, sprite As Image, offset As Point, type As Integer)
        MyBase.New(position.X, position.Y)
        Me.sprite = sprite
        Me.type = type
        grabbed = True
        Form1.grabLock = True
        grabOffset = offset
        isNew = True
    End Sub

    Public Overrides Sub tick()
        MyBase.tick()
    End Sub

    Public Overrides Sub onRelease()
        If isOverlapping() Then
            If Not safePosition.X = -404 Then ' Completely arbitrary number to see if a safe position exists
                x = safePosition.X
                y = safePosition.Y
            End If
            If isOverlapping() Then ' An edge case that will most likely occur when a new cauldron is bought but spawn on top of an existing one.
                ' Tries to find empty space around
                Dim overlapping As Boolean = True
                Dim lastPosition As Point = New Point(x, y)
                While isOverlapping() ' Tries each direction sequentially to find space (left, right, up, down)
                    x -= 20
                    If x < 0 Then
                        x = lastPosition.X
                        While isOverlapping()
                            x += 20
                            If x + sprite.Width > Form1.Width Then
                                x = lastPosition.X
                                While isOverlapping()
                                    y -= 20
                                    If y < 0 Then
                                        y = lastPosition.Y
                                        While isOverlapping()
                                            y += 20
                                            If y + sprite.Height > Form1.Height Then ' Worst case scenario where no empty space exists to the left (only if the player creates a line of cauldrons to block the new one), defaults to middle
                                                x = Form1.Width / 2 - sprite.Width / 2
                                                y = Form1.Height / 2 - sprite.Height / 2
                                                safePosition = New Point(x, y)
                                                Exit While
                                            End If
                                        End While
                                    End If
                                End While
                            End If
                        End While
                    End If
                End While
            End If ' This amount of indents is horribly grotesque but I am too lazy to find a way to condense it so here we are
        Else
            safePosition = New Point(x, y)
        End If
    End Sub

    Private Function isOverlapping() As Boolean ' Tests for overlap with other cauldrons
        For Each cauldron In Form1.gameObjects.OfType(Of EditCauldron)
            If Not cauldron.Equals(Me) AndAlso getBounds.IntersectsWith(cauldron.getBounds()) Then Return True
        Next
        Return False
    End Function

    Public Sub transmute() ' Turns the cauldron back into a functioning one for the daytime
        Form1.newObjects.AddLast(New Cauldron(Me))
        Form1.deadObjects.AddLast(Me)
    End Sub

    Public Overrides Sub render(g As Graphics)
        MyBase.render(g)
        Dim iconBackground As SolidBrush = New SolidBrush(Color.FromArgb(200, 10, 10, 10))
        Dim selectedBackground As SolidBrush = New SolidBrush(Color.FromArgb(200, 200, 200, 200))
        Dim iconRect As Rectangle = New Rectangle(x - 30 + sprite.Width / 2, y - 50 + sprite.Height / 2, 60, 60)
        Dim sellRect As Rectangle = New Rectangle(x - 30 + sprite.Width / 2, y + 20 + sprite.Height / 2, 60, 60)
        g.FillEllipse(iconBackground, iconRect)
        If Not Form1.grabLock AndAlso getBounds().Contains(Form1.MousePosition) AndAlso
            Not sellRect.Contains(Form1.MousePosition) Then g.FillEllipse(selectedBackground, iconRect)
        If grabbed AndAlso isOverlapping() Then
            g.DrawImage(My.Resources.CancelIcon, iconRect)
        Else
            g.DrawImage(My.Resources.MoveIcon, iconRect)
            If Not grabbed Then
                g.FillEllipse(iconBackground, sellRect)
                If sellRect.Contains(Form1.MousePosition) Then
                    Dim sellPen As Pen = New Pen(Color.Black, 8)
                    sellPen.LineJoin = LineJoin.Bevel
                    grabPrimed = False ' As tick happens before render, grabPrimed is set to false here so that clicking the sell button
                    ' takes priority over being grabbed
                    Dim price As Integer
                    Select Case type
                        Case 1
                            price = 500
                        Case 2
                            price = 1000
                        Case 3
                            price = 2000
                    End Select
                    If Not isNew Then price *= 0.75 ' Used cauldrons from the previous day don't give a full refund
                    g.FillEllipse(selectedBackground, sellRect)
                    Form1.outlineText(g, FormatCurrency(price, 0), Form1.pfc.Families(0), 40, Brushes.White, sellPen,
                                      New Point(x + 30 + sprite.Width / 2, y + 50 + sprite.Height / 2), StringAlignment.Near)
                    If Not Form1.mouseLock AndAlso Form1.MouseButtons = MouseButtons.Left Then
                        Form1.money.addValue(price)
                        Form1.grabLock = False
                        Form1.deadObjects.AddLast(Me)
                        My.Computer.Audio.Play(My.Resources.money, AudioPlayMode.Background)
                    End If
                    sellPen.Dispose()
                End If
                g.DrawImage(My.Resources.SellIcon, sellRect)
            End If
        End If
        iconBackground.Dispose()
        selectedBackground.Dispose()
    End Sub

End Class

Public Class Order
    Inherits Draggable

    Dim timerStart As DateTime
    Dim duration As Integer = 60000
    Dim cooldown As Integer
    Dim orderOpen As Boolean ' Kept independently of potion-hovering effects (which can temporarily open an order)
    Dim orderHeight As AnimatedValue = New AnimatedValue(40)
    Dim active, isNew As Boolean
    Dim orderImg As Bitmap
    Public recipe(0 To 2, 0 To 3) As Integer
    Public potionHover As Boolean

    Public Sub New(x As Integer, y As Integer)
        MyBase.New(x, y)
        sprite = My.Resources.PaperRoll
        orderHeight.duration = 5
        reactivate()
    End Sub

    Public Sub New(position As Point, offset As Point)
        ' Used only when an new parchment is bought at night
        MyBase.New(position.X, position.Y)
        sprite = My.Resources.PaperRoll
        grabbed = True
        Form1.grabLock = True
        grabOffset = offset
        orderOpen = False
        potionHover = False
        active = False
        orderHeight.duration = 5
        orderHeight.snapValue(0)
        orderImg = New Bitmap(219, 268) ' Just sets it as a blank bitmap so that rendering doesn't throw issues (will be updated when the day starts)
        isNew = True
        Randomize()
    End Sub

    Public Sub reactivate()
        orderOpen = True
        potionHover = False
        active = True
        Dim largeOrder As Boolean = (Form1.day > 5 AndAlso Int(Rnd() * 4) = 0) ' Day 5 and onward, orders can spawn with copius amounts of ingredients
        recipe = {{0, 0, 0, 0}, {0, 0, 0, 0}, {0, 0, 0, 0}}
        recipe(0, Int(Rnd() * 4)) += 1 ' Ensure that there is at least one ingredient in the green stage of brewing (as brewing must start in green)
        For stage = 0 To 2
            For ingredient = 0 To 3
                recipe(stage, ingredient) += Int(Rnd() * 2)
                If largeOrder Then recipe(stage, ingredient) += Int(Rnd() * 4)
            Next
        Next
        timerStart = DateTime.Now
        createOrderImg()
        orderHeight.setValue(275)
        My.Computer.Audio.Play(My.Resources.orderUp, AudioPlayMode.Background)
    End Sub

    Public Overrides Sub deactivate()
        active = False
        orderHeight.setValue(0)
        cooldown = getTimer()
        timerStart = DateTime.Now
        isNew = False
    End Sub

    Public Function getPaperRect() As Rectangle
        Return New Rectangle(x + 3, y + 19, My.Resources.OrderBackground.Width, orderHeight.getValue())
    End Function

    Private Function getTimer() As Integer
        Return (DateTime.Now.TimeOfDay - timerStart.TimeOfDay).TotalMilliseconds
    End Function

    Private Sub createOrderImg()
        ' The actual order is a bitmap image that is created when the order is instantiated, with all the necessary
        ' ingredients/parts rendered onto it here so that it can be rendered at runtime as just an image, significantly
        ' improving performance.
        ' https://learn.microsoft.com/en-us/dotnet/desktop/winforms/advanced/how-to-create-a-bitmap-at-run-time?view=netframeworkdesktop-4.8
        orderImg = New Bitmap(219, 268)
        Dim g As Graphics = Graphics.FromImage(orderImg)
        Dim textPen As Pen = New Pen(Color.Black, 5)
        g.SmoothingMode = SmoothingMode.AntiAlias
        textPen.LineJoin = LineJoin.Bevel ' Fixes a glitch where the 1 and 4 glyphs are shaped in such a way that the corners created sharp spikes ("thorns")
        ' by rounding corners
        ' https://stackoverflow.com/questions/44683841/infinity-angles-on-sharp-corners-in-graphics
        g.DrawImage(My.Resources.OrderBackground, New Rectangle(0, 0, 219, 268))
        For h = 0 To 2 ' Iterating through brew stages (columns)
            Dim emptyOffset As Integer = 0
            For k = 0 To 3 ' Iterating (rendering) through all four ingredients (in a column)
                If recipe(h, k) = 0 Then
                    emptyOffset += 1 ' If an ingredient shouldn't be added, shift back the y offset of all subsequent ingredients in the column by 1 (45)
                Else
                    Select Case k
                        Case 0
                            g.DrawImage(My.Resources.Shroom, New Rectangle(20 + (64 * h), 61 + (45 * (k - emptyOffset)), 40, 40))
                        Case 1
                            g.DrawImage(My.Resources.Herb, New Rectangle(20 + (64 * h), 61 + (45 * (k - emptyOffset)), 40, 40))
                        Case 2
                            g.DrawImage(My.Resources.Eye, New Rectangle(20 + (64 * h), 61 + (45 * (k - emptyOffset)), 40, 40))
                        Case 3
                            g.DrawImage(My.Resources.Crystal, New Rectangle(20 + (64 * h), 61 + (45 * (k - emptyOffset)), 40, 40))
                    End Select
                    Form1.outlineText(g, recipe(h, k), Form1.pfc.Families(0), 20, Brushes.White, textPen, New Point(62.5 + (64 * h), 91 + (45 * (k - emptyOffset))),
                                      StringAlignment.Far)
                End If
            Next
        Next
        textPen.Dispose()
    End Sub

    Public Overrides Sub tick()
        MyBase.tick()
        If Not active Then Return
        If Not grabbed AndAlso Not Form1.mouseLock AndAlso Form1.MouseButtons = MouseButtons.Left AndAlso
            New Rectangle(x, y + orderHeight.getValue(), sprite.Width, sprite.Height).Contains(Form1.MousePosition) Then
            orderOpen = Not orderOpen
        End If
        If potionHover Or orderOpen Then
            orderHeight.setValue(275)
        Else
            orderHeight.setValue(40)
        End If
    End Sub

    Public Overrides Sub render(g As Graphics)
        orderHeight.updateValue()
        If active Or Not orderHeight.done Then
            g.FillRectangle(New SolidBrush(Color.FromArgb(201, 162, 103)), getPaperRect())

            If orderHeight.getValue() > 50 Then
                g.SetClip(getPaperRect())

                g.DrawImage(orderImg, New Rectangle(x + 3, y + 19, 219, 268))

                g.ResetClip()
            End If
            Dim bottomRoll As Image = My.Resources.RollUp
            If Not (orderOpen Or potionHover) Then bottomRoll = My.Resources.RollDown
            g.DrawImage(bottomRoll, New Rectangle(x, y + orderHeight.getValue(), sprite.Width, sprite.Height))
        End If
        MyBase.render(g)
        If Form1.night Then
            Dim iconBackground As SolidBrush = New SolidBrush(Color.FromArgb(200, 10, 10, 10))
            Dim selectedBackground As SolidBrush = New SolidBrush(Color.FromArgb(200, 200, 200, 200))
            Dim sellRect As Rectangle = New Rectangle(x - 30 + sprite.Width / 2, y + 20 + sprite.Height / 2, 60, 60)
            If Not grabbed Then
                g.FillEllipse(iconBackground, sellRect)
                If sellRect.Contains(Form1.MousePosition) Then
                    Dim sellPen As Pen = New Pen(Color.Black, 8)
                    Dim price As Integer = 50 * (2 ^ (Form1.gameObjects.OfType(Of Order).Count() - 1))
                    If Not isNew Then price *= 0.75
                    sellPen.LineJoin = LineJoin.Bevel
                    grabPrimed = False ' As tick happens before render, grabPrimed is set to false here so that clicking the sell button
                    ' takes priority over being grabbed
                    g.FillEllipse(selectedBackground, sellRect)
                    Form1.outlineText(g, FormatCurrency(price, 0), Form1.pfc.Families(0), 40, Brushes.White, sellPen,
                                      New Point(x + 30 + sprite.Width / 2, y + 50 + sprite.Height / 2), StringAlignment.Near)
                    If Not Form1.mouseLock AndAlso Form1.MouseButtons = MouseButtons.Left Then
                        Form1.money.addValue(price)
                        Form1.grabLock = False
                        Form1.deadObjects.AddLast(Me)
                        My.Computer.Audio.Play(My.Resources.money, AudioPlayMode.Background)
                    End If
                    sellPen.Dispose()
                End If
                g.DrawImage(My.Resources.SellIcon, sellRect)
            End If
            iconBackground.Dispose()
            selectedBackground.Dispose()
            Return
        End If
        Dim timerBrush As SolidBrush = New SolidBrush(Color.FromArgb(148, 72, 208))
        If active Then
            Form1.cleanArc(g, timerBrush, x + sprite.Width / 2, y + 8 + sprite.Height / 2, 33, 21, 180, Math.Min(getTimer() / duration * 180, 180))
            If getTimer() > duration Then deactivate()
        Else
            Form1.cleanArc(g, timerBrush, x + sprite.Width / 2, y + 8 + sprite.Height / 2, 33, 21, 180, Math.Max((cooldown - getTimer() * 2) / duration * 180, 0))
            If cooldown - getTimer() * 2 < -10 Then reactivate() ' Reopen order after cooling down
        End If
        timerBrush.Dispose()
        If potionHover AndAlso orderHeight.getValue() > 274 Then g.DrawImage(My.Resources.SellOverlay, New Rectangle(x, y, sprite.Width, sprite.Height + 275))
    End Sub

End Class